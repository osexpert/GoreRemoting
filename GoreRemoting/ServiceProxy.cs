using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Castle.DynamicProxy;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using Grpc.Core;
using Grpc.Net.Compression;
using stakx.DynamicProxy;

namespace GoreRemoting;

public class ServiceProxy<T> : AsyncInterceptor
{
	RemotingClient _client;
	string _serviceName;
	
	public ServiceProxy(RemotingClient client)
	{
		_client = client;
		_serviceName = typeof(T).Name;
	}

	protected override void Intercept(IInvocation invocation)
	{
		var args = invocation.Arguments;
		var targetMethod = invocation.Method;

		_client._serviceMethodLookup.TryAdd((_serviceName, targetMethod.Name), targetMethod);

		// Check if return type is IAsyncEnumerable<T>
		var returnType = targetMethod.ReturnType;
		if (AsyncEnumerableHelper.IsAsyncEnumerable(_client, returnType, out var elementType))
		{
			invocation.ReturnValue = HandleAsyncEnumerableReturn(targetMethod, args, elementType);
			return;
		}

		(var arguments, var cancelArgument, var streamingDelePos) = MapArguments(targetMethod, args);

		var headers = new Metadata();
		var serializer = ChooseSerializer(typeof(T), targetMethod);
		var compressor = ChooseCompressor(typeof(T), targetMethod);

		// context? A guid?
		_client._config.BeforeCall?.Invoke(new BeforeCallArgs(typeof(T), targetMethod, headers, serializer, compressor));

		//headers.Add(Constants.SerializerHeaderKey, serializer.Name);
		//if (compressor != null)
		//	headers.Add(Constants.CompressorHeaderKey, compressor.EncodingName);

		var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
			targetMethod: targetMethod,
			args: arguments
			);

		var requestMsg = new GoreRequestMessage(callMessage, _serviceName, targetMethod.Name, serializer, compressor);

		var resultMessage = _client.Invoke(requestMsg,
			(callback, res) => HandleResponseAsync(serializer, compressor, callback, res, args, streamingDelePos, cancelArgument),
			new CallOptions(headers: headers, cancellationToken: cancelArgument));

		if (resultMessage.IsException)
			throw GoreSerializer.RestoreSerializedException(_client._config.ExceptionStrategy, serializer, resultMessage.Value!);

		var parameterInfos = targetMethod.GetParameters();

		foreach (var outArgument in resultMessage.OutArguments)
		{
			var parameterInfo = parameterInfos[outArgument.Position];
			//.Single(p => p.Name == outArgument.ParameterName);

			// GetElementType() https://stackoverflow.com/a/738281/2671330
			if (!parameterInfo.IsOutParameterForReal())
				throw new Exception("Impossible: out arg but not IsOut");
			args[parameterInfo.Position] = outArgument.OutValue;
		}

		invocation.ReturnValue = resultMessage.Value;

		// restore context flow from server
		CallContext.RestoreFromChangesSnapshot(resultMessage.CallContextSnapshot);
	}

	protected override async ValueTask InterceptAsync(IAsyncInvocation invocation)
	{
		var args = invocation.Arguments.ToArray();
		var targetMethod = invocation.Method;

		_client._serviceMethodLookup.TryAdd((_serviceName, targetMethod.Name), targetMethod);

		(var arguments, var cancelArgument, var streamingDelePos) = MapArguments(targetMethod, args);

		var headers = new Metadata();
		var serializer = ChooseSerializer(typeof(T), targetMethod);
		var compressor = ChooseCompressor(typeof(T), targetMethod);

		_client._config.BeforeCall?.Invoke(new BeforeCallArgs(typeof(T), targetMethod, headers, serializer, compressor));

		//headers.Add(Constants.SerializerHeaderKey, serializer.Name);
		//if (compressor != null)
		//	headers.Add(Constants.CompressorHeaderKey, compressor.EncodingName);

		var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
			targetMethod: targetMethod,
			args: arguments
			);

		var requestMsg = new GoreRequestMessage(callMessage, _serviceName, targetMethod.Name, serializer, compressor);

		var resultMessage = await _client.InvokeAsync(requestMsg,
			(callback, req) => HandleResponseAsync(serializer, compressor, callback, req, args.ToArray(), streamingDelePos, cancelArgument),
			new CallOptions(headers: headers, cancellationToken: cancelArgument)).ConfigureAwait(false);

		if (resultMessage.IsException)
			throw GoreSerializer.RestoreSerializedException(_client._config.ExceptionStrategy, serializer, resultMessage.Value!);

		// out|ref not possible with async

		invocation.Result = resultMessage.Value;

		// restore context flow from server
		CallContext.RestoreFromChangesSnapshot(resultMessage.CallContextSnapshot);
	}

	private object HandleAsyncEnumerableReturn(MethodInfo targetMethod, object?[] args, Type elementType)
	{
		// Use reflection to call the generic method
		var method = GetType()
			.GetMethod(nameof(StreamFromServerAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
			.MakeGenericMethod(elementType);

		return method.Invoke(this, [targetMethod, args])!;
	}

	private async IAsyncEnumerable<TElement> StreamFromServerAsync<TElement>(
		MethodInfo targetMethod,
		object?[] args
		)
	{
		var serializer = ChooseSerializer(typeof(T), targetMethod);
		var compressor = ChooseCompressor(typeof(T), targetMethod);
		var headers = new Metadata();

		_client._config.BeforeCall?.Invoke(new BeforeCallArgs(typeof(T), targetMethod, headers, serializer, compressor));

		(var arguments, var cancelArgument, var streamingDelePos) = MapArguments(targetMethod, args);

		var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
			targetMethod: targetMethod,
			args: arguments
		);

		var requestMsg = new GoreRequestMessage(callMessage, _serviceName, targetMethod.Name, serializer, compressor);

		var res = AsyncEnumerableAdapter.FromPush<TElement>(async func =>
		{
			var resultMessage = await _client.InvokeAsync(requestMsg,
				async (callback, req) =>
				{
					if (callback.ResponseType == ResponseType.AsyncEnumReturnResult)
					{
						await func((TElement)callback.AsyncEnumReturnResult.Value!).ConfigureAwait(false);
						return null; // Keep going
					}
					else
					{
						// Otherwise delegate to normal handler
						var result = await HandleResponseAsync(serializer, compressor, callback, req, args, streamingDelePos, cancelArgument).ConfigureAwait(false);
						return result;
					}
				},
				new CallOptions(headers: headers, cancellationToken: /*combinedCancellation*/ cancelArgument)).ConfigureAwait(false);

			if (resultMessage.IsException)
				throw GoreSerializer.RestoreSerializedException(_client._config.ExceptionStrategy, serializer, resultMessage.Value!);
		});

		await foreach (var r in res.WithCancellation(cancelArgument).ConfigureAwait(false))
		{
			yield return r;
		}
	}

	private ISerializerAdapter ChooseSerializer(Type t, MethodInfo mi)
	{
		var st = ChooseSerializerType(t, mi);

		if (st == null)
			throw new Exception("Serializer not set");

		return _client._config.GetSerializerByType(st);
	}

	private Type? ChooseSerializerType(Type t, MethodInfo mi)
	{
		// check method...
		var a1 = mi.GetCustomAttribute<SerializerAttribute>();
		if (a1 != null)
			return a1.Serializer;

		// ...then service itself
		var t1 = t.GetCustomAttribute<SerializerAttribute>();
		if (t1 != null)
			return t1.Serializer;

		// else default
		return _client._config.DefaultSerializer;
	}

	private ICompressionProvider? ChooseCompressor(Type t, MethodInfo mi)
	{
		var ct = ChooseCompressorType(t, mi);

		if (ct == null || ct == typeof(NoCompressionProvider))
			return null;

		return _client._config.GetCompressorByType(ct);
	}

	private Type? ChooseCompressorType(Type t, MethodInfo mi)
	{
		// check method...
		var a1 = mi.GetCustomAttribute<CompressorAttribute>();
		if (a1 != null)
			return a1.Compressor;

		// ...then service itself
		var t1 = t.GetCustomAttribute<CompressorAttribute>();
		if (t1 != null)
			return t1.Compressor;

		// else check default
		return _client._config.DefaultCompressor;
	}

	private async Task<MethodResultMessage?> HandleResponseAsync(
		ISerializerAdapter serializer, 
		ICompressionProvider? compressor, 
		GoreResponseMessage callbackData,
		Func<GoreRequestMessage, Task> res, 
		object?[] args,
		HashSet<int> streamingDelegatePosition,
		CancellationToken cancel
		)
	{
		// WEIRD BUT...callbackData also has serializer and compressor!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! This make no sense.
		// It kind of make sense:
		// callbackData.Serializer is the serializer used by who sent the message to us.
		// serializer (the argument) is the serialized we will use when we send the message.

		switch (callbackData.ResponseType)
		{
			case ResponseType.MethodResult:
				return callbackData.MethodResult;

			case ResponseType.AsyncEnumCall:
				{
					var aeMsg = callbackData.AsyncEnumCall;
					var arg = args[aeMsg.Position]!;

					AsyncEnumerableHelper.IsAsyncEnumerable(_client, arg.GetType(), out var enumElementType);
	
					// Handle IAsyncEnumerable streaming from client to server
					await HandleAsyncEnumerableProduceAsync(arg, enumElementType!, aeMsg, serializer, compressor, res, callbackData, cancel).ConfigureAwait(false);
					return null;
				}

			case ResponseType.DelegateCall:
				{
					var delegateMsg = callbackData.DelegateCall;
					var delegt = (Delegate)args[delegateMsg.Position]!;
					await HandleDelegateCall(serializer, compressor, callbackData, res, streamingDelegatePosition, delegateMsg, delegt).ConfigureAwait(false);
					return null;
				}
			default:
				throw new Exception($"Unknown repose type: {callbackData.ResponseType}");
		}
	}

	private async Task HandleDelegateCall(
		ISerializerAdapter serializer,
		ICompressionProvider? compressor, 
		GoreResponseMessage callbackData, 
		Func<GoreRequestMessage, Task> res, 
		HashSet<int> streamingDelegatePosition, 
		DelegateCallMessage delegateMsg, 
		Delegate delegt
		)
	{
		StreamingStatus streamingStatus = streamingDelegatePosition.Contains(delegateMsg.Position) ? StreamingStatus.Active : StreamingStatus.None;

		do
		{
			// not possible with async here?
			object? result = null;
			object? exception = null;

			try
			{
				result = delegt.DynamicInvoke(delegateMsg.Arguments);
				result = await TaskResultHelper.GetTaskResult(delegt.Method, result).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				if (ex is TargetInvocationException tie)
					ex2 = tie.InnerException;

				if (ex2 is StreamingDoneException)
				{
					if (streamingStatus != StreamingStatus.Active)
						throw new Exception("Streaming not active");
					streamingStatus = StreamingStatus.Done;
					result = null; // important!
				}
				else
				{
					if (delegateMsg.OneWay)
					{
						// eat...
						_client.OnOneWayException(ex);
					}
					else
					{
						exception = GoreSerializer.GetSerializableException(serializer, ex2);
					}
				}
			}

			if (delegateMsg.OneWay)
				return;// (flowControl: false, value: null);

			DelegateResultMessage msg;
			if (exception != null)
			{
				msg = new DelegateResultMessage
				{
					Position = delegateMsg.Position,
					ParameterName = delegateMsg.ParameterName,
					Value = exception,
					ResultType = DelegateResultType.Exception
				};
			}
			else
			{
				msg = new DelegateResultMessage
				{
					Position = delegateMsg.Position,
					ParameterName = delegateMsg.ParameterName,
					Value = result,
					StreamingStatus = streamingStatus,
				};
			}

			var requestMsg = new GoreRequestMessage(msg, callbackData.ServiceName, callbackData.MethodName, serializer, compressor);
			await res(requestMsg).ConfigureAwait(false);

			if (exception != null)
				return;
		}
		while (streamingStatus == StreamingStatus.Active);// && exception == null);

		//return (flowControl: true, value: null);
	}

	private async Task HandleAsyncEnumerableProduceAsync(
		object asyncEnumerable,
		Type elementType,
		AsyncEnumCallMessage aeMsg,
		ISerializerAdapter serializer,
		ICompressionProvider? compressor,
		Func<GoreRequestMessage, Task> res,
		GoreResponseMessage callbackData,
		CancellationToken cancel
		)
	{
		var method = GetType()
			.GetMethod(nameof(HandleAsyncEnumerableProduceAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
			.MakeGenericMethod(elementType);

		await (Task)method.Invoke(this,
			[asyncEnumerable, aeMsg, serializer, compressor, res, callbackData, cancel])!;
	}

	private async Task HandleAsyncEnumerableProduceAsyncGeneric<TElement>(
		IAsyncEnumerable<TElement> asyncEnumerable,
		AsyncEnumCallMessage aeMsg,
		ISerializerAdapter serializer,
		ICompressionProvider? compressor,
		Func<GoreRequestMessage, Task> res,
		GoreResponseMessage callbackData,
		CancellationToken cancel
		)
	{
		// Server is requesting the next item
		// We need to iterate and send each item back

		Exception ex = null!;

		var asyncEnumerableExceptionHandler = new AsyncEnumerableExceptionHandler<TElement>(asyncEnumerable, exept => ex = exept);

		await foreach (var item in asyncEnumerableExceptionHandler.WithCancellation(cancel).ConfigureAwait(false))
		{
			var msg = new AsyncEnumCallResultMessage
			{
				Position = aeMsg.Position,
				ParameterName = aeMsg.ParameterName,
				Value = item,
			};

			var requestMsg = new GoreRequestMessage(msg, callbackData.ServiceName, callbackData.MethodName, serializer, compressor);
			await res(requestMsg).ConfigureAwait(false);
		}

		AsyncEnumCallResultMessage msgDone;

		if (ex != null)
		{
			Exception ex2 = ex;
			if (ex is TargetInvocationException tie)
				ex2 = tie.InnerException;

			var exception = GoreSerializer.GetSerializableException(serializer, ex2);

			msgDone = new AsyncEnumCallResultMessage
			{
				Position = aeMsg.Position,
				ParameterName = aeMsg.ParameterName,
				Value = exception,
				ResultType = DelegateResultType.Exception
			};
		}
		else
		{
			// Send completion signal
			msgDone = new AsyncEnumCallResultMessage
			{
				Position = aeMsg.Position,
				ParameterName = aeMsg.ParameterName,
				Value = null,
				StreamingDone = true,
			};
		}

		var doneRequestMsg = new GoreRequestMessage(msgDone, callbackData.ServiceName, callbackData.MethodName, serializer, compressor);
		await res(doneRequestMsg).ConfigureAwait(false);
	}

	/// <summary>
	/// Maps non serializable arguments into a serializable form.
	/// </summary>
	/// <param name="arguments">Arguments</param>
	/// <returns>Array of arguments (includes mapped ones)</returns>
	private (object?[] arguments, CancellationToken cancelArgument, HashSet<int> streamingDelePos) MapArguments(MethodInfo mi, object?[] arguments)
	{
		CancellationToken? cancelArgument = null;
		HashSet<int> streamingDelePos = new();

		object?[] res = new object?[arguments.Length];

		var methodParams = mi.GetParameters();

		for (int i = 0; i < arguments.Length; i++)
		{
			var argument = arguments[i];

			var type = argument?.GetType();

			if (type == null)
			{
				res[i] = argument;
			}
			// Check for IAsyncEnumerable<T> parameter (client-to-server streaming)
			else if (AsyncEnumerableHelper.IsAsyncEnumerable(_client, type, out _))
			{
				// Create a marker that won't be serialized - similar to RemoteDelegateInfo for delegates
				var asyncEnumMarker = new RemoteAsyncEnumPlaceholder();
				// Store marker in serializable args, keep original in args for HandleResponseAsync
				res[i] = asyncEnumMarker;
			}
			else if (MapDelegateArgument(type, out var mappedArgument))
			{
				if (mappedArgument.HasResult)
				{
					var streamingDele = methodParams[i].GetCustomAttribute<StreamingFuncAttribute>();
					if (streamingDele != null)
					{
						streamingDelePos.Add(i);
					}
				}

				res[i] = mappedArgument;
			}
			else if (typeof(CancellationToken).IsAssignableFrom(type))
			{
				if (cancelArgument != null)
					throw new Exception("Only one CancellationToken argument is supported");
				else
					cancelArgument = (CancellationToken)argument!;

				res[i] = new CancellationTokenPlaceholder();
			}
			else
			{
				res[i] = argument;
			}
		}

		return (res, cancelArgument ?? default, streamingDelePos);
	}

	/// <summary>
	/// Maps a delegate argument into a serializable RemoteDelegateInfo object.
	/// </summary>
	/// <param name="argumentType">Type of argument to be mapped</param>
	/// <param name="mappedArgument">Out: Mapped argument</param>
	/// <returns>True if mapping applied, otherwise false</returns>
	private bool MapDelegateArgument(Type argumentType, [NotNullWhen(returnValue: true)] out RemoteDelegateInfo? mappedArgument)
	{
		if (!typeof(Delegate).IsAssignableFrom(argumentType))
		{
			mappedArgument = null;
			return false;
		}

		var delegateReturnType = argumentType.GetMethod("Invoke").ReturnType;

		var remoteDelegateInfo =
			new RemoteDelegateInfo(
				// TODO: use a OneWay attribute instead?
				hasResult: !(delegateReturnType == typeof(void) || delegateReturnType == typeof(Task) || delegateReturnType == typeof(ValueTask))
				);

		mappedArgument = remoteDelegateInfo;
		return true;
	}
}
