using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using Grpc.Core;
using Grpc.Net.Compression;
using KPreisser;
using Nerdbank.Streams;

namespace GoreRemoting;


public class RemotingServer : IRemotingParty
{

	readonly MethodCallMessageBuilder MethodCallMessageBuilder = new();

	//private ConcurrentDictionary<(Type, int), DelegateProxy> _delegateProxyCache = new();
	readonly ConcurrentDictionary<string, Type> _services = new();

	ConcurrentDictionary<(MethodInfo, MessageType, int), Type[]> IRemotingParty.TypesCache { get; } = new ConcurrentDictionary<(MethodInfo, MessageType, int), Type[]>();

	readonly ConcurrentDictionary<(string, string), MethodInfo> _serviceMethodCache = new();

	readonly ServerConfig _config;

	public RemotingServer(ServerConfig config)
	{
		_config = config;

		DuplexCallDescriptor = Descriptors.GetDuplexCall(config.GrpcServiceName, "DuplexCall",
			Marshallers.Create<GoreRequestMessage>(SerializeRequest, DeserializeRequest),
			Marshallers.Create<GoreResponseMessage>(SerializeResponse, DeserializeResponse)
			);
	}

	private void SerializeResponse(GoreResponseMessage arg, SerializationContext sc)
	{
		try
		{
			using (var s = sc.GetBufferWriter().AsStream())
			{
				var bw = new GoreBinaryWriter(s);

				bw.Write((byte)Constants.SerializationVersion); // version

				bw.Write((byte)arg.ResponseType);

				bw.Write(arg.Serializer.Name);
				bw.Write(arg.Compressor?.EncodingName ?? string.Empty);
				bw.Write(arg.ServiceName);
				bw.Write(arg.MethodName);

				MethodInfo method = GetServiceMethod(arg.ServiceName, arg.MethodName);

				arg.Serialize(this, s, method);
			}
		}
		finally
		{
			sc.Complete();
		}
	}

	private GoreRequestMessage DeserializeRequest(DeserializationContext arg)
	{
		using var s = arg.PayloadAsReadOnlySequence().AsStream();

		var br = new GoreBinaryReader(s);

		byte version = br.ReadByte();
		if (version != Constants.SerializationVersion)
			throw new Exception($"Unsupported version: {version}");

		var mType = (RequestType)br.ReadByte();

		var serializerName = br.ReadString();
		var compressorName = br.ReadString();
		var serviceName = br.ReadString();
		var methodName = br.ReadString();

		var serializer = _config.GetSerializerByName(serializerName);

		ICompressionProvider? compressor = null;
		if (!string.IsNullOrEmpty(compressorName))
		{
			compressor = _config.GetCompressorByName(compressorName);
		}

		MethodInfo method = GetServiceMethod(serviceName, methodName);

		return GoreRequestMessage.Deserialize(this, s, mType, serviceName, methodName, method, serializer, compressor);
	}

	private void SerializeRequest(GoreRequestMessage arg, SerializationContext sc)
	{
		throw new NotSupportedException();
	}

	private GoreResponseMessage DeserializeResponse(DeserializationContext arg)
	{
		throw new NotSupportedException();
	}

	private MethodInfo GetServiceMethod(string serviceName, string methodName)
	{
		if (_serviceMethodCache.TryGetValue((serviceName, methodName), out var mi))
			return mi;

		var serviceInterfaceType = GetServiceType(serviceName);

		var all_methods = serviceInterfaceType.GetMethods();

		var methods = all_methods.Where(m => m.Name == methodName);

		if (!methods.Any())
			throw new MissingMethodException($"Method not found: {serviceName}.{methodName}");
		else if (methods.Count() > 1)
			throw new MissingMethodException($"More than one method found: {serviceName}.{methodName} (method names must be unique)");
		else if (methods.Single().IsGenericMethod)
			throw new MissingMethodException($"Generic method not supported: {serviceName}.{methodName}");
		var method = methods.Single();

		_serviceMethodCache.TryAdd((serviceName, methodName), method);

		return method;
	}

	private ServiceHandle GetService(string serviceName, ServerCallContext context)
	{
		if (!_services.TryGetValue(serviceName, out var serviceType))
			throw new Exception($"Service not registered: {serviceName}");

		var service = _config.CreateService(serviceType, context);
		return service;
	}

	private Type GetServiceType(string serviceName)
	{
		if (_services.TryGetValue(serviceName, out var serviceType))
			return serviceType;

		throw new Exception($"Service not registered: {serviceName}");
	}

	/// <summary>
	/// Maps non serializable arguments into a serializable form.
	/// </summary>
	/// <param name="arguments">Array of parameter values</param>
	/// <returns>Array of arguments (includes mapped ones)</returns>
	private object?[] MapArguments(
		object?[] arguments, 
		(Type type, string name)[] typesAndNames,
		Func<DelegateCallMessage, Task<object?>> callDelegateAsync,
		Func<AsyncEnumCallMessage, Task<(object? value, bool isDone)>> callAsyncEnumAsync,
		ServerCallContext context)
	{
		object?[] mappedArguments = new object?[arguments.Length];

		for (int i = 0; i < arguments.Length; i++)
		{
			var argument = arguments[i];
			var typeAndName = typesAndNames[i];

			if (MapDelegateArgument(argument, typeAndName, i, out var mappedArgument, callDelegateAsync))
				mappedArguments[i] = mappedArgument;
			else if (MapAsyncEnumArgument(argument, typeAndName, i, out var mappedArgument2, callAsyncEnumAsync))
				mappedArguments[i] = mappedArgument2;
			else if (argument is CancellationTokenPlaceholder)
				mappedArguments[i] = context.CancellationToken;
			else
				mappedArguments[i] = argument;
		}

		return mappedArguments;
	}

	/// <summary>
	/// Maps a delegate argument into a delegate proxy.
	/// </summary>
	/// <param name="argument">argument value</param>
	/// <param name="position"></param>
	/// <param name="mappedArgument">Out: argument value where delegate value is mapped into delegate proxy</param>
	/// <returns>True if mapping applied, otherwise false</returns>
	/// <exception cref="ArgumentNullException">Thrown if no session is provided</exception>
	private bool MapDelegateArgument(
		object? argument,
		(Type type, string name) typeAndName,
		int position,
		[NotNullWhen(returnValue: true)] out object? mappedArgument,
		Func<DelegateCallMessage, Task<object?>> callDelegateAsync)
	{
		if (argument is not RemoteDelegateInfo remoteDelegateInfo)
		{
			mappedArgument = null;
			return false;
		}

		//if (false)//_delegateProxyCache.ContainsKey((delegateType, position)))
		//{
		//	mappedArgument = _delegateProxyCache[(delegateType, position)].ProxiedDelegate;
		//	return true;
		//}

		// Forge a delegate proxy and initiate remote delegate invocation, when it is invoked
		var delegateProxy =
			new DelegateProxy(typeAndName.type,
			(delegateArgs) =>
			{
				var r = callDelegateAsync(new DelegateCallMessage
				{
					Arguments = delegateArgs,
					Position = position,
					ParameterName = typeAndName.name,
					OneWay = !remoteDelegateInfo.HasResult
				}).GetAwaiter().GetResult();

				return r;
			},
			(delegateArgs) => // async
			{
				var r = callDelegateAsync(new DelegateCallMessage
				{
					Arguments = delegateArgs,
					ParameterName = typeAndName.name,
					Position = position,
					OneWay = !remoteDelegateInfo.HasResult
				});
				return r;
			});

		// TODO: do we need cache?
		//			_delegateProxyCache.TryAdd((delegateType, position), delegateProxy);

		mappedArgument = delegateProxy.ProxiedDelegate;
		return true;
	}



	private bool MapAsyncEnumArgument(
		object? argument,
		(Type type, string name) typeAndName,
		int position,
		[NotNullWhen(returnValue: true)] out object? mappedArgument,
		Func<AsyncEnumCallMessage, Task<(object? value, bool isDone)>> callAsyncEnumAsync
		)
	{
		if (argument is not RemoteAsyncEnumPlaceholder remoteDelegateInfo)
		{
			mappedArgument = null;
			return false;
		}

		// Check if this is an IAsyncEnumerable<T> parameter
		AsyncEnumerableHelper.IsAsyncEnumerable(typeAndName.type, out var elementType);

		// Create a proxy that pulls data from the client
		var method = typeof(AsyncEnumerableProxy)
			.GetMethod(nameof(AsyncEnumerableProxy.Create))!
			.MakeGenericMethod(elementType);

		// Create typed pull function
		var typedPullFunc = CreateTypedPullFunc(() =>
		{
			// Call back to client to make client start streaming us result messages
			return callAsyncEnumAsync(new AsyncEnumCallMessage
			{
				ParameterName = typeAndName.name,
				Position = position
			});

		}, elementType!);

		mappedArgument = method.Invoke(null, [typedPullFunc])!;
		return true;

	}

	private object CreateTypedPullFunc(Func<Task<(object?, bool)>> pullFunc, Type elementType)
	{
		var method = typeof(RemotingServer)
			.GetMethod(nameof(TypedPullFuncWrapper), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(elementType);

		return method.Invoke(null, [pullFunc])!;
	}

	private static Func<Task<(T, bool)>> TypedPullFuncWrapper<T>(Func<Task<(object?, bool)>> pullFunc)
	{
		return async () =>
		{
			var (obj, isDone) = await pullFunc().ConfigureAwait(false);
			return ((T)obj!, isDone);
		};
	}

	public void RegisterService<TInterface, TService>()
	{
		var iface = typeof(TInterface);

		if (!iface.IsInterface)
			throw new Exception($"{iface.Name} is not an interface");

		if (!_services.TryAdd(iface.Name, typeof(TService)))
			throw new Exception($"Service already added: {iface.Name}");
	}

	class DuplexCallState
	{
//		public bool ResultSent;
		public int? ActiveStreamingDelegatePosition;
	}

	private async Task DuplexCall(
		GoreRequestMessage request,
		Func<Task<GoreRequestMessage>> req, 
		Func<GoreResponseMessage, Task> reponse, 
		ServerCallContext context
		)
	{
		var callMessage = request.MethodCallMessage;

		CallContext.RestoreFromChangesSnapshot(callMessage.CallContextSnapshot);

		var parameterTypes = request.Method.GetParameters().Select(p => (p.ParameterType, p.Name)).ToArray();
		var parameterValues = callMessage.ParameterValues();

		using var responseLock = new ResponseLock();
		DuplexCallState state = new();

		parameterValues = MapArguments(
			parameterValues, 
			parameterTypes,
			(delegateCallMsg) => DelegateCallAsync(request, req, reponse, delegateCallMsg, state, responseLock),
			(asyncEnumCallMsg) => AsyncEnumCallAsync(request, req, reponse, asyncEnumCallMsg, state, responseLock),
			context
			);

		object? result = null;
		ServiceHandle? serviceHandle = null;
		Exception? ex2 = null;
		ICallScope? callScope = null;

		try
		{
			serviceHandle = GetService(request.ServiceName, context);
			var service = serviceHandle.Value.Service;

			callScope = _config.CreateCallScope?.Invoke();
			callScope?.Start(context, request.ServiceName, request.MethodName, service, request.Method, parameterValues);

			result = request.Method.Invoke(service, parameterValues);
			result = await TaskResultHelper.GetTaskResult(request.Method, result).ConfigureAwait(false);

			var returnType = request.Method.ReturnType;
			if (AsyncEnumerableHelper.IsAsyncEnumerable(returnType, out var _))
			{
				// Handle IAsyncEnumerable<T> return by streaming results
				await HandleAsyncEnumerableReturnAsync(result, request, reponse, state, responseLock, context.CancellationToken).ConfigureAwait(false);
				result = null;
			}

			callScope?.Success(result);
		}
		catch (Exception ex)
		{
			ex2 = ex;
			if (ex2 is TargetInvocationException tie)
				ex2 = tie.InnerException;

			callScope?.Failure(ex2);
		}
		finally
		{
			callScope?.Dispose();
			callScope = null;

			if (serviceHandle != null)
				await _config.ReleaseService(serviceHandle.Value).ConfigureAwait(false);
		}

		MethodResultMessage resultMessage;

		if (ex2 == null)
		{
			resultMessage =
				MethodCallMessageBuilder.BuildMethodCallResultMessage(
						method: request.Method,
						args: parameterValues,
						returnValue: result
						);
		}
		else
		{
			var serEx = GoreSerializer.GetSerializableException(request.Serializer, ex2);
			resultMessage = new MethodResultMessage { Value = serEx, ResultType = MethodResultType.Exception };
		}

		var responseMsg = new GoreResponseMessage(resultMessage, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);

		// This will block new responses and wait until existing ones have left. We then get exlusive lock and set the flag.
		await responseLock.RundownResponsesAsync().ConfigureAwait(false);

		await reponse(responseMsg).ConfigureAwait(false);
	}

	private async Task HandleAsyncEnumerableReturnAsync(
		object? asyncEnumerable,
		GoreRequestMessage request,
		Func<GoreResponseMessage, Task> response,
		DuplexCallState state,
		ResponseLock responseLock,
		CancellationToken cancel
		)
	{
		if (asyncEnumerable == null)
			throw new InvalidOperationException("IAsyncEnumerable result is null");

		// Use reflection to call the generic helper
		var enumType = asyncEnumerable.GetType();
		AsyncEnumerableHelper.IsAsyncEnumerable(enumType, out var elementType);

		var method = typeof(RemotingServer)
			.GetMethod(nameof(StreamAsyncEnumerableToClient), BindingFlags.NonPublic | BindingFlags.Instance)!
			.MakeGenericMethod(elementType);

		var task = (Task)method.Invoke(this, [asyncEnumerable, request, response, state, responseLock, cancel]);
		await task.ConfigureAwait(false);
	}

	private async Task StreamAsyncEnumerableToClient<T>(
		IAsyncEnumerable<T> asyncEnumerable,
		GoreRequestMessage request,
		Func<GoreResponseMessage, Task> response,
		DuplexCallState state,
		ResponseLock responseLock,
		CancellationToken cancel
		)
	{
		// No need to use exceptino wrapper, if we fail the exception will propagate back to DuplexCall and catched there and use regular method result message.

		await foreach (var item in asyncEnumerable.WithCancellation(cancel).ConfigureAwait(false))
		{
			var resultMessage = new AsyncEnumReturnResultMessage
			{
				Value = item,
			};

			var responseMsg = new GoreResponseMessage(
				resultMessage,
				request.ServiceName,
				request.MethodName,
				request.Serializer,
				request.Compressor);

			await responseLock.EnterResponseAsync().ConfigureAwait(false);
			try
			{
				await response(responseMsg).ConfigureAwait(false);
			}
			finally
			{
				responseLock.ExitResponse();
			}
		}
	}

	private async Task<object?> DelegateCallAsync(
		GoreRequestMessage request,
		Func<Task<GoreRequestMessage>> req,
		Func<GoreResponseMessage, Task> reponse,
		DelegateCallMessage delegateCallMsg,
		DuplexCallState state,
		ResponseLock responseLock
		)
	{
		// send respose to client and client will call the delegate via DelegateProxy
		// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
		// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
		// 2: we send and do not even wait for the sending to complete. (currently not implemented)
		await responseLock.EnterResponseAsync().ConfigureAwait(false);
		try
		{
			if (delegateCallMsg.Position == state.ActiveStreamingDelegatePosition)
			{
				// only recieve now that streaming is active
			}
			else
			{
				var delegateCallReponseMsg = new GoreResponseMessage(delegateCallMsg, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);
				await reponse(delegateCallReponseMsg).ConfigureAwait(false);
			}

			// FIXME: Task, ValueTask etc? as well as void?
			if (delegateCallMsg.OneWay)
			{
				// fire and forget. no result, not even exception
				return null;
			}
			else
			{
				// we want result or exception
				var reqMsg = await req().ConfigureAwait(false);

				var msg = reqMsg.DelegateResultMessage;

				if (msg.Position != delegateCallMsg.Position)
					throw new Exception("Incorrect result position");

				if (msg.IsException)
					throw GoreSerializer.RestoreSerializedException(_config.ExceptionStrategy, request.Serializer, msg.Value!);

				if (msg.StreamingStatus == StreamingStatus.Active)
					state.ActiveStreamingDelegatePosition = msg.Position;
				else if (msg.StreamingStatus == StreamingStatus.Done)
				{
					state.ActiveStreamingDelegatePosition = null;
					throw new StreamingDoneException();
				}

				return msg.Value;
			}
		}
		finally
		{
			responseLock.ExitResponse();
		}
	}


	private async Task<(object? value, bool isDone)> AsyncEnumCallAsync(
		GoreRequestMessage request,
		Func<Task<GoreRequestMessage>> req,
		Func<GoreResponseMessage, Task> reponse,
		AsyncEnumCallMessage delegateCallMsg,
		DuplexCallState state,
		ResponseLock responseLock
		)
	{
		// send respose to client and client will call the delegate via DelegateProxy
		// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
		// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
		// 2: we send and do not even wait for the sending to complete. (currently not implemented)
		await responseLock.EnterResponseAsync().ConfigureAwait(false);
		try
		{
			if (state.ActiveStreamingDelegatePosition == delegateCallMsg.Position)
			{
				// only recieve now that streaming is active
			}
			else
			{
				var delegateCallReponseMsg = new GoreResponseMessage(delegateCallMsg, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);
				await reponse(delegateCallReponseMsg).ConfigureAwait(false);
			}

			// we want result or exception
			var reqMsg = await req().ConfigureAwait(false);

			var msg = reqMsg.AsyncEnumCallResultMessage;

			if (msg.Position != delegateCallMsg.Position)
				throw new Exception("Incorrect result position");

			if (msg.IsException)
				throw GoreSerializer.RestoreSerializedException(_config.ExceptionStrategy, request.Serializer, msg.Value!);

			if (msg.StreamingDone)
				state.ActiveStreamingDelegatePosition = null;
			else
				state.ActiveStreamingDelegatePosition = msg.Position;

			return (msg.Value, isDone: msg.StreamingDone);
		}
		finally
		{
			responseLock.ExitResponse();
		}
	}


	public Method<GoreRequestMessage, GoreResponseMessage> DuplexCallDescriptor { get; }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="requestStream"></param>
	/// <param name="responseStream"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	public async Task DuplexCall(
		IAsyncStreamReader<GoreRequestMessage> requestStream,
		IServerStreamWriter<GoreResponseMessage> responseStream, 
		ServerCallContext context
		)
	{
		try
		{
			bool gotNext = await requestStream.MoveNext().ConfigureAwait(false);
			if (!gotNext)
				throw new Exception("No method call request data");

			// TODO: pass context.CancellationToken here?
			using var threadSafeResponseStream = new GoreRemoting.ThreadSafeStreamWriter<GoreResponseMessage>(responseStream);//, _config.ResponseQueueLength);

			await this.DuplexCall(
				requestStream.Current,
				async () =>
				{
					var gotNext = await requestStream.MoveNext(threadSafeResponseStream.OnErrorToken).ConfigureAwait(false);
					if (!gotNext)
						throw new Exception("No delegate request data");
					return requestStream.Current;
				},
				resp => threadSafeResponseStream.WriteAsync(resp).AsTask(),
				context
				).ConfigureAwait(false);

			await threadSafeResponseStream.CompleteAsync().ConfigureAwait(false);
		}
		catch (Exception e)
		{
			context.Status = new Status(StatusCode.Unknown, e.ToString());
		}
	}
}

/// <summary>
/// 
/// </summary>
public class Descriptors
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="serviceName"></param>
	/// <param name="methodName"></param>
	/// <returns></returns>
	public static Method<GoreRequestMessage, GoreResponseMessage> GetDuplexCall(string serviceName, string methodName, Marshaller<GoreRequestMessage> marshallerReq,
		Marshaller<GoreResponseMessage> marshallerRes)

	{
		return new Method<GoreRequestMessage, GoreResponseMessage>(
			type: MethodType.DuplexStreaming,
			serviceName: serviceName,
			name: methodName,
			requestMarshaller: marshallerReq,
			responseMarshaller: marshallerRes);
	}
}
