﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Grpc.Core;
using GrpcRemoting.RemoteDelegates;
using GrpcRemoting.RpcMessaging;
using GrpcRemoting.Serialization;
using stakx.DynamicProxy;

namespace GrpcRemoting
{
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

			var arguments = MapArguments(args);

			var serializer = _client.DefaultSerializer;

			var headers = new Metadata();

			_client.BeforeMethodCall(typeof(T), targetMethod, headers, ref serializer);

			headers.Add(Constants.SerializerHeaderKey, serializer.Name);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				serializer: serializer, 
				remoteServiceName: _serviceName, 
				targetMethod: targetMethod, 
				args: arguments);

			var wireCallMsg = new WireCallMessage() { Data = callMessage };

			var bytes = serializer.Serialize(wireCallMsg);

			var resultMessage = _client.Invoke(bytes, 
				(callback, res) => HandleResponseAsync(serializer, callback, res, args), 
				new CallOptions(headers: headers));

			if (resultMessage.Exception != null)
				throw resultMessage.Exception.Capture();

			var parameterInfos = targetMethod.GetParameters();

			foreach (var outParameterValue in resultMessage.OutParameters)
			{
				var parameterInfo = parameterInfos.First(p => p.Name == outParameterValue.ParameterName);
				args[parameterInfo.Position] = outParameterValue.IsOutValueNull ? null : outParameterValue.OutValue;
			}

			invocation.ReturnValue = resultMessage.ReturnValue;

            //CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
        }

		protected override async ValueTask InterceptAsync(IAsyncInvocation invocation)
		{
			var args = invocation.Arguments;
			var targetMethod = invocation.Method;

			var arguments = MapArguments(args);

			var serializer = _client.DefaultSerializer;

			var headers = new Metadata();

			_client.BeforeMethodCall(typeof(T), targetMethod, headers, ref serializer);

			headers.Add(Constants.SerializerHeaderKey, serializer.Name);

			var callMessage = _client.MethodCallMessageBuilder.BuildMethodCallMessage(
				serializer: serializer, 
				remoteServiceName: _serviceName, 
				targetMethod: targetMethod, 
				args: arguments);

			var wireCallMsg = new WireCallMessage() { Data = callMessage };

			var bytes = serializer.Serialize(wireCallMsg);

			var resultMessage =  await _client.InvokeAsync(bytes,
				(callback, req) => HandleResponseAsync(serializer, callback, req, args.ToArray()),
				new CallOptions(headers: headers)).ConfigureAwait(false);

			if (resultMessage.Exception != null)
				throw resultMessage.Exception.Capture();

			// out|ref not possible with async

			invocation.Result = resultMessage.ReturnValue;

            //CallContext.RestoreFromSnapshot(resultMessage.CallContextSnapshot);
        }

		private async Task<MethodCallResultMessage> HandleResponseAsync(ISerializerAdapter serializer, byte[] callback, Func<byte[], Task> res, object[] args)
		{
			var callbackData = serializer.Deserialize<WireResponseMessage>(callback);

			switch (callbackData.ResponseType)
			{
				case ResponseType.Result:
					return (MethodCallResultMessage)callbackData.Data;

				case ResponseType.Delegate:
					{
						var delegateMsg = (DelegateCallMessage)callbackData.Data;

						var delegt = (Delegate)args[delegateMsg.Position];

						// not possible with async here?
						object result = null;
						Exception exception = null;

						try
						{
							// FIXME: but we need to know if the delegate has a result or not???!!!
							result = delegt.DynamicInvoke(delegateMsg.Arguments);
						}
						catch (Exception ex) when (!delegateMsg.OneWay) // PS: not eating exceptions here. what happen to the exception??
						{
							Exception ex2 = null;
							if (ex is TargetInvocationException tie)
								ex2 = tie.InnerException;

							exception = ex2.GetType().IsSerializable ? ex2 : new RemoteInvocationException(ex2.Message);
						}

						if (delegateMsg.OneWay)
							return null;

						DelegateCallResultMessage msg = null;
						if (exception != null)
							msg = new DelegateCallResultMessage() { Exception = exception };
						else
							msg = new DelegateCallResultMessage() { Result = result };

						var data = serializer.Serialize(msg);
						await res(data).ConfigureAwait(false);
					}
					break;
				default:
					throw new Exception();
			}

			return null;
		}

		/// <summary>
		/// Maps non serializable arguments into a serializable form.
		/// </summary>
		/// <param name="arguments">Arguments</param>
		/// <returns>Array of arguments (includes mapped ones)</returns>
		private object[] MapArguments(IEnumerable<object> arguments)
		{
			return arguments.Select(argument =>
			{
				var type = argument?.GetType();

				if (MapDelegateArgument(type, argument, out var mappedArgument))
					return mappedArgument;
				else
					return argument;

			}).ToArray();
		}

		/// <summary>
		/// Maps a delegate argument into a serializable RemoteDelegateInfo object.
		/// </summary>
		/// <param name="argumentType">Type of argument to be mapped</param>
		/// <param name="argument">Argument to be wrapped</param>
		/// <param name="mappedArgument">Out: Mapped argument</param>
		/// <returns>True if mapping applied, otherwise false</returns>
		private bool MapDelegateArgument(Type argumentType, object argument, out object mappedArgument)
		{
			if (argumentType == null || !typeof(Delegate).IsAssignableFrom(argumentType))
			{
				mappedArgument = argument;
				return false;
			}

			var delegateReturnType = argumentType.GetMethod("Invoke").ReturnType;

			var remoteDelegateInfo =
				new RemoteDelegateInfo(
					delegateTypeName: argumentType.FullName, 
					hasResult: delegateReturnType != typeof(void));

			mappedArgument = remoteDelegateInfo;
			return true;
		}


  
    }

    internal static class ExceptionExtensions
    {
		internal static Exception Capture(this Exception e)
		{
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(e, e.StackTrace + System.Environment.NewLine);

			return e;
		}

    }
}
