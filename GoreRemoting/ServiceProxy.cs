using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Grpc.Core;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using stakx.DynamicProxy;
using System.IO;
using Grpc.Net.Compression;

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

			(var arguments, var cancel, var streamingDelePos) = MapArguments(targetMethod, args);

			var headers = new Metadata();
			var serializer = ChooseSerializer(typeof(T), targetMethod);
			var compressor = ChooseCompressor(typeof(T), targetMethod);

			_client._config.BeforeMethodCall?.Invoke(new BeforeMethodCallParams(typeof(T), targetMethod, headers, serializer, compressor));

			//headers.Add(Constants.SerializerHeaderKey, serializer.Name);
			//if (compressor != null)
			//	headers.Add(Constants.CompressorHeaderKey, compressor.EncodingName);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				remoteServiceName: _serviceName,
				targetMethod: targetMethod,
				args: arguments,
				setCallContext: _client._config.SetCallContext
				);

			var requestMsg = new GoreRequestMessage(callMessage, serializer, compressor);

			var resultMessage = _client.Invoke(requestMsg,
				(callback, res) => HandleResponseAsync(serializer, compressor, callback, res, args, streamingDelePos),
				new CallOptions(headers: headers, cancellationToken: cancel));

			if (resultMessage.Exception != null)
				throw serializer.RestoreSerializedException(resultMessage.Exception);

			var parameterInfos = targetMethod.GetParameters();

			foreach (var outArgument in resultMessage.OutArguments)
			{
				var parameterInfo = parameterInfos.First(p => p.Name == outArgument.ParameterName);
				args[parameterInfo.Position] = outArgument.OutValue;
			}

			invocation.ReturnValue = resultMessage.ReturnValue;

			// restore context flow from server
			if (_client._config.RestoreCallContext)
				CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
		}

		async ValueTask InterceptAsync(IAsyncInvocation invocation)
		{
			var args = invocation.Arguments.ToArray();
			var targetMethod = invocation.Method;

			(var arguments, var cancel, var streamingDelePos) = MapArguments(targetMethod, args);

			var headers = new Metadata();
			var serializer = ChooseSerializer(typeof(T), targetMethod);
			var compressor = ChooseCompressor(typeof(T), targetMethod);

			_client._config.BeforeMethodCall?.Invoke(new BeforeMethodCallParams(typeof(T), targetMethod, headers, serializer, compressor));

			//headers.Add(Constants.SerializerHeaderKey, serializer.Name);
			//if (compressor != null)
			//	headers.Add(Constants.CompressorHeaderKey, compressor.EncodingName);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				remoteServiceName: _serviceName,
				targetMethod: targetMethod,
				args: arguments,
				setCallContext: _client._config.SetCallContext
				);

			var requestMsg = new GoreRequestMessage(callMessage, serializer, compressor);

			var resultMessage = await _client.InvokeAsync(requestMsg,
				(callback, req) => HandleResponseAsync(serializer, compressor, callback, req, args.ToArray(), streamingDelePos),
				new CallOptions(headers: headers, cancellationToken: cancel)).ConfigureAwait(false);

			if (resultMessage.Exception != null)
				throw serializer.RestoreSerializedException(resultMessage.Exception);

			// out|ref not possible with async

			invocation.Result = resultMessage.ReturnValue;

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

		private Type ChooseSerializerType(Type t, MethodInfo mi)
		{
			// check method...
			var a1 = mi.GetCustomAttribute<SerializerAttribute>();
			if (a1 != null)
				return a1.Serializer;

			// ...then service itself
			var t1 = t.GetCustomAttribute<SerializerAttribute>();
			if (t1 != null)
				return a1.Serializer;

			// else default
			return _client._config.DefaultSerializer;
		}

		private ICompressionProvider ChooseCompressor(Type t, MethodInfo mi)
		{
			var ct = ChooseCompressorType(t, mi);

			if (ct == null || ct == typeof(NoCompressionProvider))
				return null;

			return _client._config.GetCompressorByType(ct);
		}

		private Type ChooseCompressorType(Type t, MethodInfo mi)
		{
			// check method...
			var a1 = mi.GetCustomAttribute<CompressorAttribute>();
			if (a1 != null)
				return a1.Compressor;

			// ...then service itself
			var t1 = t.GetCustomAttribute<CompressorAttribute>();
			if (t1 != null)
				return a1.Compressor;

			// else check default
			return _client._config.DefaultCompressor;
		}



		private async Task<MethodResultMessage> HandleResponseAsync(ISerializerAdapter serializer, ICompressionProvider compressor, GoreResponseMessage callbackData,
			Func<GoreRequestMessage, Task> res, object[] args,
			int? streamingDelegatePosition)
		{
			switch (callbackData.ResponseType)
			{
				case ResponseType.MethodResult:
					return callbackData.MethodResult;

				case ResponseType.DelegateCall:
					{
						var delegateMsg = callbackData.DelegateCall;

						var delegt = (Delegate)args[delegateMsg.Position];

						StreamingStatus streamingStatus = (streamingDelegatePosition == delegateMsg.Position) ? StreamingStatus.Active : StreamingStatus.None;

					again:

						// not possible with async here?
						object result = null;
						object exception = null;

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
							msg = new DelegateResultMessage { Position = delegateMsg.Position, Exception = exception };
						else
							msg = new DelegateResultMessage { Position = delegateMsg.Position, Result = result, StreamingStatus = streamingStatus };

						var requestMsg = new GoreRequestMessage(msg, serializer, compressor);

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
		private (object[] arguments, CancellationToken cancel, int? streamingDelePos) MapArguments(MethodInfo mi, object[] arguments)
		{
			bool delegateHasResult = false;

			CancellationToken? lastCancel = null;

			int? streamingDelePos = null;

			object[] res = new object[arguments.Length];

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
						lastCancel = (CancellationToken)argument;

					res[i] = new CancellationTokenDummy();
				}
				else
					res[i] = argument;
			}

			return (res, lastCancel ?? default, streamingDelePos);
		}

		/// <summary>
		/// Maps a delegate argument into a serializable RemoteDelegateInfo object.
		/// </summary>
		/// <param name="argumentType">Type of argument to be mapped</param>
		/// <param name="mappedArgument">Out: Mapped argument</param>
		/// <returns>True if mapping applied, otherwise false</returns>
		private bool MapDelegateArgument(Type argumentType, out RemoteDelegateInfo mappedArgument)
		{
			if (argumentType == null || !typeof(Delegate).IsAssignableFrom(argumentType))
			{
				mappedArgument = null;
				return false;
			}

			var delegateReturnType = argumentType.GetMethod("Invoke").ReturnType;

			var remoteDelegateInfo =
				new RemoteDelegateInfo(
					delegateTypeName: TypeShortener.GetShortType(argumentType),
					// TODO: use a OneWay attribute instead?
					hasResult: !(delegateReturnType == typeof(void) || delegateReturnType == typeof(Task) || delegateReturnType == typeof(ValueTask))
					);

			mappedArgument = remoteDelegateInfo;
			return true;
		}
	}


	internal static class TaskResultHelper
	{
		public static async Task<object> GetTaskResult(MethodInfo method, object resultIn)
		{
			object resultOut = resultIn;

			if (resultIn != null)
			{
				var returnType = method.ReturnType;

				if (returnType == typeof(Task))
				{
					var resultTask = (Task)resultIn;
					await resultTask.ConfigureAwait(false);
					resultOut = null;
				}
				else if (returnType == typeof(ValueTask))
				{
					var resultTask = (ValueTask)resultIn;
					await resultTask.ConfigureAwait(false);
					resultOut = null;
				}
				else if (returnType.IsGenericType)
				{
					if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
					{
						var resultTask = (Task)resultIn;
						await resultTask.ConfigureAwait(false);

						resultOut = returnType.GetProperty("Result")?.GetValue(resultIn); // why '?' ?
					}
					else if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
					{
						//await ToTask((dynamic)result).ConfigureAwait(false);

						//var resultTask = (dynamic)result;
						//await resultTask.ConfigureAwait(false);

						var valueTaskToTask = typeof(TaskResultHelper)
							.GetMethod(nameof(ValueTaskToTask), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
							.MakeGenericMethod(returnType.GenericTypeArguments.Single());

						await (Task)valueTaskToTask.Invoke(null, new[] { resultIn });

						resultOut = returnType.GetProperty("Result")?.GetValue(resultIn); // why '?' ?
					}
				}
			}

			return resultOut;
		}

		//private static Task<T> ToTask<T>(ValueTask<T> task)
		//{
		//	return task.AsTask();
		//}

		private static Task ValueTaskToTask<T>(ValueTask<T> task)
		{
			return task.AsTask();
		}

	}
}
