using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using GoreRemoting.Serialization.Binary;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading;
using KPreisser;

namespace GoreRemoting
{

	public class RemotingServer
	{

		MethodCallMessageBuilder MethodCallMessageBuilder = new();

		//private ConcurrentDictionary<(Type, int), DelegateProxy> _delegateProxyCache = new();
		ConcurrentDictionary<string, Type> _services = new();

		ServerConfig _config;

        public RemotingServer(ServerConfig config)
		{
            _config = config;
        }

		private object GetService(string serviceName, ServerCallContext context)
		{
			if (!_services.TryGetValue(serviceName, out var serviceType))
				throw new Exception("Service not registered: " + serviceName);

			if (_config.CreateInstance != null)
				return _config.CreateInstance(serviceType, context.RequestHeaders);
			else
				return Activator.CreateInstance(serviceType);
        }

		private Type GetServiceType(string serviceName)
		{
			if (_services.TryGetValue(serviceName, out var serviceType))
				return serviceType;

			throw new Exception("Service not registered: " + serviceName);
		}

		/// <summary>
		/// Maps non serializable arguments into a serializable form.
		/// </summary>
		/// <param name="arguments">Array of parameter values</param>
		/// <param name="callDelegate"></param>
		/// <returns>Array of arguments (includes mapped ones)</returns>
		private object[] MapArguments(object[] arguments, 
			Func<DelegateCallMessage, object> callDelegate, 
			Func<DelegateCallMessage, Task<object>> callDelegateAsync,
			ServerCallContext context)
		{
			object[] mappedArguments = new object[arguments.Length];

			for (int i = 0; i < arguments.Length; i++)
			{
				var argument = arguments[i];

				if (MapDelegateArgument(argument, i, out var mappedArgument, callDelegate, callDelegateAsync))
					mappedArguments[i] = mappedArgument;
				else if (argument is CancellationTokenDummy)
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
		/// <param name="callDelegate"></param>
		/// <returns>True if mapping applied, otherwise false</returns>
		/// <exception cref="ArgumentNullException">Thrown if no session is provided</exception>
		private bool MapDelegateArgument(object argument, int position, out object mappedArgument, Func<DelegateCallMessage, object> callDelegate,
			Func<DelegateCallMessage, Task<object>> callDelegateAsync)
		{
			if (!(argument is RemoteDelegateInfo remoteDelegateInfo))
			{
				mappedArgument = argument;
				return false;
			}

			var delegateType = Type.GetType(remoteDelegateInfo.DelegateTypeName);
            if (delegateType == null)
                throw new Exception("Delegate type not found: " + remoteDelegateInfo.DelegateTypeName);

            //if (false)//_delegateProxyCache.ContainsKey((delegateType, position)))
            //{
            //	mappedArgument = _delegateProxyCache[(delegateType, position)].ProxiedDelegate;
            //	return true;
            //}

            // Forge a delegate proxy and initiate remote delegate invocation, when it is invoked
            var delegateProxy =
				new DelegateProxy(delegateType, 
				delegateArgs => 
				{
					var r = callDelegate(new DelegateCallMessage { Arguments = delegateArgs, Position = position, OneWay = !remoteDelegateInfo.HasResult });
					return r;
				},
				delegateArgs => // async
				{
					var r = callDelegateAsync(new DelegateCallMessage { Arguments = delegateArgs, Position = position, OneWay = !remoteDelegateInfo.HasResult });
					return r;
				});

			// TODO: do we need cache?
//			_delegateProxyCache.TryAdd((delegateType, position), delegateProxy);

			mappedArgument = delegateProxy.ProxiedDelegate;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void RegisterService<TInterface, TService>()
        {
			var iface = typeof(TInterface);

            if (!iface.IsInterface)
				throw new Exception($"{iface.Name} is not interface");

            if (!_services.TryAdd(iface.Name, typeof(TService)))
				throw new Exception("Service already added: " + iface.Name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="iface"></param>
		//public void RegisterService(Type type, Type iface) => RegisterService(type, iface.Name);

        private async Task DuplexCall(ISerializerAdapter serializer, byte[] request, Func<Task<byte[]>> req, Func<byte[], Task> reponse, ServerCallContext context)
		{
            var callMessage = serializer.Deserialize<MethodCallMessage>(request);

			//CallContext.RestoreFromSnapshot(callMessage.CallContextSnapshot);

			(var parameterValues, var parameterTypes) = callMessage.UnwrapParametersFromDeserializedMethodCallMessage();

			bool resultSent = false;
			var responseLock = new AsyncReaderWriterLockSlim();

			parameterValues = MapArguments(parameterValues,
			delegateCallMsg =>
			{
				var delegateResultMessage = new WireResponseMessage(delegateCallMsg);

				// send respose to client and client will call the delegate via DelegateProxy
				// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
				// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
				// 2: we send and do not even wait for the sending to complete. (currently not implemented)
				responseLock.EnterReadLock();
				try
				{
					if (resultSent)
						throw new Exception("Too late, result sent");

					reponse(serializer.Serialize(delegateResultMessage)).GetAwaiter().GetResult();

					if (delegateCallMsg.OneWay)
					{
						// fire and forget. no result, not even exception
						return null;
					}
					else
					{
						// we want result or exception
						byte[] data = req().GetAwaiter().GetResult();
						var msg = serializer.Deserialize<DelegateResultMessage>(data);
						if (msg.Position != delegateCallMsg.Position)
							throw new Exception("Incorrect result position");

						if (msg.Exception != null)
							throw msg.Exception.Capture();
						else
							return msg.Result;
					}
				}
				finally
				{
					responseLock.ExitReadLock();
				}
			}, 
			async delegateCallMsg =>
			{
				var delegateResultMessage = new WireResponseMessage(delegateCallMsg);

				// send respose to client and client will call the delegate via DelegateProxy
				// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
				// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
				// 2: we send and do not even wait for the sending to complete. (currently not implemented)
				await responseLock.EnterReadLockAsync().ConfigureAwait(false);
				try
				{
					if (resultSent)
						throw new Exception("Too late, result sent");

					await reponse(serializer.Serialize(delegateResultMessage)).ConfigureAwait(false);

					// FIXME: Task, ValueTask etc? as well as void?
					if (delegateCallMsg.OneWay)
					{
						// fire and forget. no result, not even exception
						return null;
					}
					else
					{
						// we want result or exception
						byte[] data = await req().ConfigureAwait(false);
						var msg = serializer.Deserialize<DelegateResultMessage>(data);
						if (msg.Position != delegateCallMsg.Position)
							throw new Exception("Incorrect result position");

						if (msg.Exception != null)
							throw msg.Exception.Capture();
						else
							return msg.Result; // Task?
					}
				}
				finally
				{
					responseLock.ExitReadLock();
				}
			},
			context);

			var serviceInterfaceType = GetServiceType(callMessage.ServiceName);
			MethodInfo method;

			if (callMessage.IsGenericMethod)
			{
				var methods = serviceInterfaceType.GetMethods();

				// only 1 method with that name is supported. no overloading support
				method =
					methods.SingleOrDefault(m =>
						m.IsGenericMethod &&
						m.Name.Equals(callMessage.MethodName, StringComparison.Ordinal));

				if (method != null)
					method = method.MakeGenericMethod(parameterTypes);
			}
			else
			{
				method =
					serviceInterfaceType.GetMethod(
						name: callMessage.MethodName,
						types: parameterTypes);
			}

			if (method == null)
				throw new MissingMethodException(
					className: callMessage.ServiceName,
					methodName: callMessage.MethodName);

			var oneWay = false;// method.GetCustomAttribute<OneWayAttribute>() != null;

			object result = null;

			Exception exception = null;

			try
			{
				var service = GetService(callMessage.ServiceName, context);
				result = method.Invoke(service, parameterValues);

				result = await TaskResultHelper.GetTaskResult(method, result);
			}
			catch (Exception ex)
			{
				if (oneWay)
				{
					// eat...
				}
				else
				{
					Exception ex2 = ex;
					if (ex2 is TargetInvocationException tie)
						ex2 = tie.InnerException;

					exception = ex2.GetType().IsSerializable ? ex2 : new RemoteInvocationException(ex2.Message);
				}
			}

			if (oneWay)
				return;

			MethodResultMessage resultMessage;

			if (exception == null)
			{
				resultMessage =
					MethodCallMessageBuilder.BuildMethodCallResultMessage(
							serializer: serializer,
							method: method,
							args: parameterValues,
							returnValue: result);
			}
			else
			{
				resultMessage = new MethodResultMessage { Exception = exception };
			}

			var methodResultMessage = new WireResponseMessage(resultMessage);

			// This will block new delegates and wait until existing ones have left. We then get exlusive lock and set the flag.
			await responseLock.EnterWriteLockAsync().ConfigureAwait(false);
			resultSent = true;
			responseLock.ExitWriteLock();

			await reponse(serializer.Serialize(methodResultMessage)).ConfigureAwait(false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="requestStream"></param>
		/// <param name="responseStream"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public Task DuplexCall(IAsyncStreamReader<byte[]> requestStream, IServerStreamWriter<byte[]> responseStream, ServerCallContext context)
		{
			var serializerName = context.RequestHeaders.GetValue(Constants.SerializerHeaderKey);

			var serializer = _config.Serializers[serializerName];

			return DuplexCall(serializer, requestStream, responseStream, context);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="requestStream"></param>
		/// <param name="responseStream"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		private async Task DuplexCall(ISerializerAdapter serializer, IAsyncStreamReader<byte[]> requestStream, IServerStreamWriter<byte[]> responseStream, ServerCallContext context)
		{
			try
			{
				var responseStreamWrapped = new GoreRemoting.StreamResponseQueue<byte[]>(responseStream);

				bool gotNext = await requestStream.MoveNext().ConfigureAwait(false);
				if (!gotNext)
					throw new Exception("No method call request data");

				await this.DuplexCall(serializer, requestStream.Current, async () =>
				{
					var gotNext = await requestStream.MoveNext().ConfigureAwait(false);
					if (!gotNext)
						throw new Exception("No delegate request data");
					return requestStream.Current;
				},
				resp => responseStreamWrapped.WriteAsync(resp).AsTask(), context).ConfigureAwait(false);

				await responseStreamWrapped.CompleteAsync().ConfigureAwait(false);
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
		public static Method<byte[], byte[]> DuplexCall = GetDuplexCall("DuplexCall");

		private static Method<byte[], byte[]> UnaryCall = GetUnaryCall("UnaryCall");

		// TODO: if a method has no delegate arguments, we could use unary call "UnaryCall", because then there is just a singlem result.
		// CON: DuplexCall will be less hot\tested less. PRO: can be faster?

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static Method<byte[], byte[]> GetDuplexCall(string name)
		{
			return new Method<byte[], byte[]>(
				type: MethodType.DuplexStreaming,
				serviceName: "GoreRemoting",
				name: name,
				requestMarshaller: Marshallers.Create(
					serializer: bytes => bytes,
					deserializer: bytes => bytes),
				responseMarshaller: Marshallers.Create(
					serializer: bytes => bytes,
					deserializer: bytes => bytes));
		}


		private static Method<byte[], byte[]> GetUnaryCall(string name)
		{
			return new Method<byte[], byte[]>(
				type: MethodType.Unary,
				serviceName: "GoreRemoting",
				name: name,
				requestMarshaller: Marshallers.Create(
					serializer: bytes => bytes,
					deserializer: bytes => bytes),
				responseMarshaller: Marshallers.Create(
					serializer: bytes => bytes,
					deserializer: bytes => bytes));
		}
	}

}
