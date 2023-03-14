﻿using System;
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

			var serializer = _client.DefaultSerializer;
			if (serializer == null)
				throw new Exception("DefaultSerializer is not set");

			var headers = new Metadata();

			_client.BeforeMethodCall(typeof(T), targetMethod, headers, ref serializer);

			headers.Add(Constants.SerializerHeaderKey, serializer.Name);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				remoteServiceName: _serviceName,
				targetMethod: targetMethod,
				args: arguments
				);

			var bytes = Gorializer.GoreSerialize(callMessage, serializer);

			var resultMessage = _client.Invoke(bytes, 
				(callback, res) => HandleResponseAsync(serializer, callback, res, args, streamingDelePos), 
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

            //CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
        }

		

		async ValueTask InterceptAsync(IAsyncInvocation invocation)
		{
			var args = invocation.Arguments.ToArray();
			var targetMethod = invocation.Method;

			(var arguments, var cancel, var streamingDelePos) = MapArguments(targetMethod, args);

			var serializer = _client.DefaultSerializer;

			var headers = new Metadata();

			_client.BeforeMethodCall(typeof(T), targetMethod, headers, ref serializer);

			headers.Add(Constants.SerializerHeaderKey, serializer.Name);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				remoteServiceName: _serviceName, 
				targetMethod: targetMethod,
				args: arguments
				);

			var bytes = Gorializer.GoreSerialize(callMessage, serializer);

			var resultMessage =  await _client.InvokeAsync(bytes,
				(callback, req) => HandleResponseAsync(serializer, callback, req, args.ToArray(), streamingDelePos),
				new CallOptions(headers: headers, cancellationToken: cancel)).ConfigureAwait(false);

			if (resultMessage.Exception != null)
				throw serializer.RestoreSerializedException(resultMessage.Exception);

			// out|ref not possible with async

			invocation.Result = resultMessage.ReturnValue;

            //CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
        }




		private async Task<MethodResultMessage> HandleResponseAsync(ISerializerAdapter serializer, byte[] callback, Func<byte[], Task> res, object[] args,
			int? streamingDelegatePosition)
		{
			var callbackData = Gorializer.GoreDeserialize<WireResponseMessage>(callback, serializer);

			switch (callbackData.ResponseType)
			{
				case ResponseType.Result:
					return callbackData.Result;

				case ResponseType.Delegate:
					{
						var delegateMsg = callbackData.Delegate;

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

							if (ex2 is StreamingFuncDone)
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
							msg = new DelegateResultMessage{ Position = delegateMsg.Position, Exception = exception };
						else
							msg = new DelegateResultMessage{ Position = delegateMsg.Position, Result = result, StreamingStatus = streamingStatus };

						var data = Gorializer.GoreSerialize(msg, serializer);

						await res(data).ConfigureAwait(false);

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
					delegateTypeName: TypeFormatter.FormatType(argumentType),
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
