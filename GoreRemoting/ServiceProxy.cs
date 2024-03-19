using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using Grpc.Core;
using Grpc.Net.Compression;
using stakx.DynamicProxy;

namespace GoreRemoting
{
	public class ServiceProxy<T> : IInterceptor // AsyncInterceptor
	{
		RemotingClient _client;
		string _serviceName;

		AsyncInterceptor _aInterceptor;

		public ServiceProxy(RemotingClient client)
		{
			_client = client;
			_serviceName = typeof(T).Name;
			_aInterceptor = new(InterceptSync, InterceptAsync);
		}

		void IInterceptor.Intercept(IInvocation invocation)
		{
			var sin = new SyncInvocation(invocation.Method, invocation.Arguments);
			_aInterceptor.Intercept(sin);
			invocation.ReturnValue = sin.ReturnValue;
		}

		void InterceptSync(ISyncInvocation invocation)
		{
			var args = invocation.Arguments;
			var targetMethod = invocation.Method;

			_client._serviceMethodLookup.TryAdd((_serviceName, targetMethod.Name), targetMethod);

			(var arguments, var cancel, var streamingDelePos) = MapArguments(targetMethod, args);

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
				args: arguments,
				emitCallContext: _client._config.EmitCallContext
				);

			var requestMsg = new GoreRequestMessage(callMessage, _serviceName, targetMethod.Name, serializer, compressor);

			var resultMessage = _client.Invoke(requestMsg,
				(callback, res) => HandleResponseAsync(serializer, compressor, callback, res, args, streamingDelePos),
				new CallOptions(headers: headers, cancellationToken: cancel));

			if (resultMessage.ResultType == ResultKind.Exception)
				throw serializer.RestoreSerializedException(resultMessage.Value!);

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
			if (_client._config.RestoreCallContext)
			{
				CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
			}
		}




		async ValueTask InterceptAsync(IAsyncInvocation invocation)
		{
			var args = invocation.Arguments.ToArray();
			var targetMethod = invocation.Method;

			_client._serviceMethodLookup.TryAdd((_serviceName, targetMethod.Name), targetMethod);

			(var arguments, var cancel, var streamingDelePos) = MapArguments(targetMethod, args);

			var headers = new Metadata();
			var serializer = ChooseSerializer(typeof(T), targetMethod);
			var compressor = ChooseCompressor(typeof(T), targetMethod);

			_client._config.BeforeCall?.Invoke(new BeforeCallArgs(typeof(T), targetMethod, headers, serializer, compressor));

			//headers.Add(Constants.SerializerHeaderKey, serializer.Name);
			//if (compressor != null)
			//	headers.Add(Constants.CompressorHeaderKey, compressor.EncodingName);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				targetMethod: targetMethod,
				args: arguments,
				emitCallContext: _client._config.EmitCallContext
				);

			var requestMsg = new GoreRequestMessage(callMessage, _serviceName, targetMethod.Name, serializer, compressor);

			var resultMessage = await _client.InvokeAsync(requestMsg,
				(callback, req) => HandleResponseAsync(serializer, compressor, callback, req, args.ToArray(), streamingDelePos),
				new CallOptions(headers: headers, cancellationToken: cancel)).ConfigureAwait(false);

			if (resultMessage.ResultType == ResultKind.Exception)
				throw serializer.RestoreSerializedException(resultMessage.Value!);

			// out|ref not possible with async

			invocation.Result = resultMessage.Value;

			// restore context flow from server
			if (_client._config.RestoreCallContext)
				CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
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



		private async Task<MethodResultMessage?> HandleResponseAsync(ISerializerAdapter serializer, ICompressionProvider? compressor, GoreResponseMessage callbackData,
			Func<GoreRequestMessage, Task> res, object?[] args,
			int? streamingDelegatePosition)
		{
			// WEIRD BUT...callbackData also has serializer and compressor!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! This make no sense.

			switch (callbackData.ResponseType)
			{
				case ResponseType.MethodResult:
					return callbackData.MethodResult;

				case ResponseType.DelegateCall:
					{
						var delegateMsg = callbackData.DelegateCall;

						var delegt = (Delegate)args[delegateMsg.Position]!;

						StreamingStatus streamingStatus = (streamingDelegatePosition == delegateMsg.Position) ? StreamingStatus.Active : StreamingStatus.None;

					again:

						// not possible with async here?
						object? result = null;
						object? exception = null;

						try
						{
							// FIXME: but we need to know if the delegate has a result or not???!!!
							result = delegt.DynamicInvoke(delegateMsg.Arguments);

							result = await TaskResultHelper.GetTaskResult(delegt.Method, result);
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
									exception = serializer.GetSerializableException(ex2);
								}
							}
						}

						if (delegateMsg.OneWay)
							return null;

						DelegateResultMessage msg;
						if (exception != null)
							msg = new DelegateResultMessage 
							{ 
								Position = delegateMsg.Position,
								ParameterName = delegateMsg.ParameterName,
								Value = exception, 
								ReturnKind = DelegateResultType.Exception 
							};
						else
							msg = new DelegateResultMessage 
							{ 
								Position = delegateMsg.Position,
								ParameterName = delegateMsg.ParameterName,
								Value = result, 
								StreamingStatus = streamingStatus,
								ReturnKind = DelegateResultType.ReturnValue
							};

						var requestMsg = new GoreRequestMessage(msg, callbackData.ServiceName, callbackData.MethodName, serializer, compressor);

						await res(requestMsg).ConfigureAwait(false);

						if (streamingStatus == StreamingStatus.Active && exception == null)
							goto again;

						return null;
					}
				default:
					throw new Exception("Unknown repose type: " + callbackData.ResponseType);
			}
		}

		/// <summary>
		/// Maps non serializable arguments into a serializable form.
		/// </summary>
		/// <param name="arguments">Arguments</param>
		/// <returns>Array of arguments (includes mapped ones)</returns>
		private (object?[] arguments, CancellationToken cancel, int? streamingDelePos) MapArguments(MethodInfo mi, object?[] arguments)
		{
			bool delegateHasResult = false;

			CancellationToken? lastCancel = null;

			int? streamingDelePos = null;

			object?[] res = new object?[arguments.Length];

			var methodParams = mi.GetParameters();

			for (int i = 0; i < arguments.Length; i++)
			{
				var argument = arguments[i];

				var type = argument?.GetType();

				if (MapDelegateArgument(type, out var mappedArgument))
				{
					if (mappedArgument.HasResult)
					{
						var streamingDele = methodParams[i].GetCustomAttribute<StreamingFuncAttribute>();
						if (streamingDele != null)
						{
							if (streamingDelePos == null)
								streamingDelePos = i;
							else
								throw new Exception("Only one streaming func delegate supported");
						}

						if (!delegateHasResult)
							delegateHasResult = true;
						else
						{
							// We could probably support more than 1, but it would complicate the logic. With max 1 the logic is easier.
							throw new Exception("Only one delegate with result is supported");
						}
					}

					res[i] = mappedArgument;
				}
				else if (typeof(CancellationToken).IsAssignableFrom(type))
				{
					if (lastCancel != null)
						throw new Exception("More than one CancellationToken");
					else
						lastCancel = (CancellationToken)argument!;

					res[i] = new CancellationTokenPlaceholder();
				}
				else
				{
					res[i] = argument;
				}
			}

			return (res, lastCancel ?? default, streamingDelePos);
		}

		/// <summary>
		/// Maps a delegate argument into a serializable RemoteDelegateInfo object.
		/// </summary>
		/// <param name="argumentType">Type of argument to be mapped</param>
		/// <param name="mappedArgument">Out: Mapped argument</param>
		/// <returns>True if mapping applied, otherwise false</returns>
		private bool MapDelegateArgument(Type? argumentType, [NotNullWhen(returnValue: true)] out RemoteDelegateInfo? mappedArgument)
		{
			if (argumentType == null || !typeof(Delegate).IsAssignableFrom(argumentType))
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



}
