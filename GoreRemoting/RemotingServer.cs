using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using GoreRemoting.RemoteDelegates;
using GoreRemoting.RpcMessaging;
using Grpc.Core;
using Grpc.Net.Compression;
using KPreisser;
using Nerdbank.Streams;

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

			DuplexCallDescriptor = Descriptors.GetDuplexCall("DuplexCall",
				Marshallers.Create<GoreRequestMessage>(SerializeRequest, DeserializeRequest),
				Marshallers.Create<GoreResponseMessage>(SerializeResponse, DeserializeResponse)
				);
		}

		private GoreRequestMessage DeserializeRequest(DeserializationContext arg)
		{
			using var s = arg.PayloadAsReadOnlySequence().AsStream();

			var br = new GoreBinaryReader(s);
			byte version = br.ReadByte();
			if (version != Constants.SerializationVersion)
				throw new Exception("Unsupported version " + version);
			var serializerName = br.ReadString();
			var compressorName = br.ReadString();
			var serviceName = br.ReadString();
			var methodName = br.ReadString();
			var mType = (RequestType)br.ReadByte();

			var serializer = _config.GetSerializerByName(serializerName);

			ICompressionProvider? compressor = null;
			if (!string.IsNullOrEmpty(compressorName))
			{
				compressor = _config.GetCompressorByName(compressorName);
			}

			MethodInfo method = GetServiceMethod(serviceName, methodName);

			return GoreRequestMessage.Deserialize(s, mType, serviceName, methodName, method, serializer, compressor);
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
				throw new MissingMethodException($"More than one method: {serviceName}.{methodName}");
			else if (methods.Single().IsGenericMethod)
				throw new MissingMethodException($"Generic method not supported: {serviceName}.{methodName}");
			var method = methods.Single();

			_serviceMethodCache.TryAdd((serviceName, methodName), method);

			return method;
		}

		ConcurrentDictionary<(string, string), MethodInfo> _serviceMethodCache = new ConcurrentDictionary<(string, string), MethodInfo>();

		private void SerializeRequest(GoreRequestMessage arg, SerializationContext sc)
		{
			throw new NotSupportedException();
		}

		private GoreResponseMessage DeserializeResponse(DeserializationContext arg)
		{
			throw new NotSupportedException();
		}

		private void SerializeResponse(GoreResponseMessage arg, SerializationContext sc)
		{
			try
			{
				using (var s = sc.GetBufferWriter().AsStream())
				{
					var bw = new GoreBinaryWriter(s);
					bw.Write((byte)Constants.SerializationVersion); // version
					bw.Write(arg.Serializer.Name);
					bw.Write(arg.Compressor?.EncodingName ?? string.Empty);
					bw.Write(arg.ServiceName);
					bw.Write(arg.MethodName);
					bw.Write((byte)arg.ResponseType);

					MethodInfo method = GetServiceMethod(arg.ServiceName, arg.MethodName);

					arg.Serialize(s, method);
				}
			}
			finally
			{
				sc.Complete();
			}
		}

		private object GetService(string serviceName, /*MethodInfo mi,*/ ServerCallContext context)
		{
			if (!_services.TryGetValue(serviceName, out var serviceType))
				throw new Exception("Service not registered: " + serviceName);

			//			var gsa = new GetServiceArgs 
			//			{ 
			//				ServiceType = serviceType, 
			////				Method = mi, 
			//				GrpcContext = context,
			////				ServiceName = serviceName
			//			};
			var service = _config.GetService(serviceType, context);
			return service;
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
		private object?[] MapArguments(object?[] arguments, Type[] types,
			Func<MethodInfo, DelegateCallMessage, object?> callDelegate,
			Func<MethodInfo, DelegateCallMessage, Task<object?>> callDelegateAsync,
			ServerCallContext context)
		{
			object?[] mappedArguments = new object?[arguments.Length];

			for (int i = 0; i < arguments.Length; i++)
			{
				var argument = arguments[i];
				var type = types[i];

				if (MapDelegateArgument(argument, type, i, out var mappedArgument, callDelegate, callDelegateAsync))
					mappedArguments[i] = mappedArgument;
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
		/// <param name="callDelegate"></param>
		/// <returns>True if mapping applied, otherwise false</returns>
		/// <exception cref="ArgumentNullException">Thrown if no session is provided</exception>
		private bool MapDelegateArgument(object? argument, Type delegateType,
			int position,
			[NotNullWhen(returnValue: true)] out object? mappedArgument,
			Func<MethodInfo, DelegateCallMessage, object?> callDelegate,
			Func<MethodInfo, DelegateCallMessage, Task<object?>> callDelegateAsync)
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
				new DelegateProxy(delegateType,
				(method, delegateArgs) =>
				{
					var r = callDelegate(method, new DelegateCallMessage { Arguments = delegateArgs, Position = position,
						ParameterName = method.Name,
						OneWay = !remoteDelegateInfo.HasResult });
					return r;
				},
				(method, delegateArgs) => // async
				{
					var r = callDelegateAsync(method, new DelegateCallMessage { Arguments = delegateArgs,
						ParameterName = method.Name,
						Position = position, OneWay = !remoteDelegateInfo.HasResult });
					return r;
				});

			// TODO: do we need cache?
			//			_delegateProxyCache.TryAdd((delegateType, position), delegateProxy);

			mappedArgument = delegateProxy.ProxiedDelegate;
			return true;
		}

		public void RegisterService<TInterface, TService>()
		{
			var iface = typeof(TInterface);

			if (!iface.IsInterface)
				throw new Exception($"{iface.Name} is not interface");

			if (!_services.TryAdd(iface.Name, typeof(TService)))
				throw new Exception("Service already added: " + iface.Name);
		}

		private async Task DuplexCall(
			GoreRequestMessage request,
			Func<Task<GoreRequestMessage>> req, Func<GoreResponseMessage, Task> reponse, ServerCallContext context)
		{
			var callMessage = request.MethodCallMessage;

			if (_config.RestoreCallContext)
				CallContext.RestoreFromSnapshot(callMessage.CallContextSnapshot);

			var parameterTypes = request.Method.GetParameters().Select(p => p.ParameterType).ToArray();

			var parameterValues = callMessage.UnwrapParametersFromDeserializedMethodCallMessage();

			bool resultSent = false;
			var responseLock = new AsyncReaderWriterLockSlim();

			int? activeStreamingDelegatePosition = null;

			parameterValues = MapArguments(parameterValues, parameterTypes,
			(method, delegateCallMsg) =>
			{
				var delegateCallResponseMsg = new GoreResponseMessage(delegateCallMsg, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);

				// send respose to client and client will call the delegate via DelegateProxy
				// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
				// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
				// 2: we send and do not even wait for the sending to complete. (currently not implemented)
				responseLock.EnterReadLock();
				try
				{
					if (resultSent)
						throw new Exception("Too late, result sent");

					if (delegateCallMsg.Position == activeStreamingDelegatePosition)
					{
						// only recieve now that streaming is active
					}
					else
					{
						reponse(delegateCallResponseMsg).GetAwaiter().GetResult();
					}

					if (delegateCallMsg.OneWay)
					{
						// fire and forget. no result, not even exception
						return null;
					}
					else
					{
						// we want result or exception
						var reqMsg = req().GetAwaiter().GetResult();

						var msg = reqMsg.DelegateResultMessage;

						if (msg.Position != delegateCallMsg.Position)
							throw new Exception("Incorrect result position");

						if (msg.ReturnKind == DelegateResultType.Exception)// msg.Exception != null)
							throw Gorializer.RestoreSerializedException(request.Serializer, msg.Value!);

						if (msg.StreamingStatus == StreamingStatus.Active)
							activeStreamingDelegatePosition = msg.Position;
						else if (msg.StreamingStatus == StreamingStatus.Done)
							throw new StreamingDoneException();

						return msg.Value;// request.Serializer.Deserialize(method.ReturnType, msg.Result);
					}
				}
				finally
				{
					responseLock.ExitReadLock();
				}
			},
			async (method, delegateCallMsg) =>
			{
				var delegateCallReponseMsg = new GoreResponseMessage(delegateCallMsg, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);

				// send respose to client and client will call the delegate via DelegateProxy
				// TODO: should we have a different kind of OneWay too, where we dont even wait for the response to be sent???
				// These may seem to be 2 varianst of OneWay: 1 where we send and wait until sent, but do not care about result\exceptions.
				// 2: we send and do not even wait for the sending to complete. (currently not implemented)
				await responseLock.EnterReadLockAsync().ConfigureAwait(false);
				try
				{
					if (resultSent)
						throw new Exception("Too late, result sent");

					if (delegateCallMsg.Position == activeStreamingDelegatePosition)
					{
						// only recieve now that streaming is active
					}
					else
					{
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

						if (msg.ReturnKind == DelegateResultType.Exception)
							throw Gorializer.RestoreSerializedException(request.Serializer, msg.Value!);

						if (msg.StreamingStatus == StreamingStatus.Active)
							activeStreamingDelegatePosition = msg.Position;
						else if (msg.StreamingStatus == StreamingStatus.Done)
							throw new StreamingDoneException();

						return msg.Value;
					}
				}
				finally
				{
					responseLock.ExitReadLock();
				}
			},
			context);



			//var oneWay = false;// method.GetCustomAttribute<OneWayAttribute>() != null;

			object? result = null;
			object? service = null;
			Exception? ex2 = null;
			ICallContext? callContext = null;

			try
			{
				service = GetService(request.ServiceName, /*method,*/ context);

				callContext = _config.CreateCallContext?.Invoke();
				callContext?.Start(context, request.ServiceName, request.MethodName, service, request.Method, parameterValues);

				result = request.Method.Invoke(service, parameterValues);
				result = await TaskResultHelper.GetTaskResult(request.Method, result);

				callContext?.Success(result);
			}
			catch (Exception ex)
			{
				//if (oneWay)
				//{
				//	// eat...
				//	//OnOneWayException(ex);
				//}
				//else
				{
					ex2 = ex;
					if (ex2 is TargetInvocationException tie)
						ex2 = tie.InnerException;
				}

				callContext?.Failure(ex2);
			}
			finally
			{
				callContext?.Dispose();
				callContext = null;
			}

			//			if (oneWay)
			//				return;

			MethodResultMessage resultMessage;

			if (ex2 == null)
			{
				resultMessage =
					MethodCallMessageBuilder.BuildMethodCallResultMessage(
							method: request.Method,
							args: parameterValues,
							returnValue: result,
							emitCallContext: _config.EmitCallContext);
			}
			else
			{
				var serEx = Gorializer.GetSerializableException(request.Serializer, ex2);
				resultMessage = new MethodResultMessage { Value = serEx, ResultType = ResultKind.Exception };
			}

			var responseMsg = new GoreResponseMessage(resultMessage, request.ServiceName, request.MethodName, request.Serializer, request.Compressor);

			// This will block new delegates and wait until existing ones have left. We then get exlusive lock and set the flag.
			await responseLock.EnterWriteLockAsync().ConfigureAwait(false);
			resultSent = true;
			responseLock.ExitWriteLock();

			await reponse(responseMsg).ConfigureAwait(false);
		}


		//public event EventHandler<Exception> OneWayException;
		//internal void OnOneWayException(Exception ex)
		//{
		//	OneWayException?.Invoke(this, ex);
		//}


		public Method<GoreRequestMessage, GoreResponseMessage> DuplexCallDescriptor { get; }




		/// <summary>
		/// 
		/// </summary>
		/// <param name="requestStream"></param>
		/// <param name="responseStream"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task DuplexCall(IAsyncStreamReader<GoreRequestMessage> requestStream,
			IServerStreamWriter<GoreResponseMessage> responseStream, ServerCallContext context)
		{
			try
			{
				var responseStreamWrapped = new GoreRemoting.StreamResponseQueue<GoreResponseMessage>(responseStream, _config.ResponseQueueLength);

				bool gotNext = await requestStream.MoveNext().ConfigureAwait(false);
				if (!gotNext)
					throw new Exception("No method call request data");

				await this.DuplexCall(requestStream.Current, async () =>
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
		/// <param name="name"></param>
		/// <returns></returns>
		public static Method<GoreRequestMessage, GoreResponseMessage> GetDuplexCall(string name, Marshaller<GoreRequestMessage> marshallerReq,
			Marshaller<GoreResponseMessage> marshallerRes)

		{
			return new Method<GoreRequestMessage, GoreResponseMessage>(
				type: MethodType.DuplexStreaming,
				serviceName: "GoreRemoting",
				name: name,
				requestMarshaller: marshallerReq,
				responseMarshaller: marshallerRes);
		}


	}




}
