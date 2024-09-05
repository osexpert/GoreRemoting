using System.Collections.Concurrent;
using System.Reflection;
using GoreRemoting.Nerdbank.Streams;
using GoreRemoting.RpcMessaging;
using Grpc.Core;
using Grpc.Net.Compression;
using static GoreRemoting.RemotingClient;

namespace GoreRemoting
{

	public class RemotingClient : IRemotingParty
	{
		internal readonly ClientConfig _config;
		readonly CallInvoker _callInvoker;
		ConcurrentDictionary<(MethodInfo, MessageType, int), Type[]> IRemotingParty.TypesCache { get; } = new ConcurrentDictionary<(MethodInfo, MessageType, int), Type[]>();

		internal ConcurrentDictionary<(string, string), MethodInfo> _serviceMethodLookup = new();


		public RemotingClient(CallInvoker callInvoker, ClientConfig config)
		{
			_config = config;
			_callInvoker = callInvoker;

			DuplexCallDescriptor = Descriptors.GetDuplexCall(config.GrpcServiceName, "DuplexCall",
				Marshallers.Create<GoreRequestMessage>(SerializeRequest, DeserializeRequest),
				Marshallers.Create<GoreResponseMessage>(SerializeResponse, DeserializeResponse)
				);
		}
		
		private void SerializeRequest(GoreRequestMessage arg, SerializationContext sc)
		{
			try
			{
				using (var s = sc.GetBufferWriter().AsStream())
				{
					var bw = new GoreBinaryWriter(s);

					bw.Write((byte)Constants.SerializationVersion); // version

					bw.Write((byte)arg.RequestType);

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

		private GoreResponseMessage DeserializeResponse(DeserializationContext arg)
		{
			using var s = arg.PayloadAsReadOnlySequence().AsStream();

			var br = new GoreBinaryReader(s);

			byte version = br.ReadByte();
			if (version != Constants.SerializationVersion)
				throw new Exception("Unsupported version " + version);

			var mType = (ResponseType)br.ReadByte();

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

			return GoreResponseMessage.Deserialize(this, s, mType, serviceName, methodName, method,
				serializer, compressor);
		}

		private GoreRequestMessage DeserializeRequest(DeserializationContext arg)
		{
			throw new NotSupportedException();
		}

		private void SerializeResponse(GoreResponseMessage arg, SerializationContext sc)
		{
			throw new NotSupportedException();
		}

		private MethodInfo GetServiceMethod(string serviceName, string methodName)
		{
			return _serviceMethodLookup[(serviceName, methodName)];
		}

		public Method<GoreRequestMessage, GoreResponseMessage> DuplexCallDescriptor { get; }


		private static readonly Castle.DynamicProxy.ProxyGenerator ProxyGenerator = new();

		public T CreateProxy<T>()
		{
			var iface = typeof(T);
			if (!iface.IsInterface)
				throw new Exception($"{iface.Name} is not an interface");

			var serviceProxyType = typeof(ServiceProxy<>).MakeGenericType(iface);
			var serviceProxy = Activator.CreateInstance(serviceProxyType, this /* RemotingClient */);

			var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget(
				interfaceToProxy: iface,
				interceptor: (Castle.DynamicProxy.IInterceptor)serviceProxy);

			return (T)proxy;
		}

		public MethodCallMessageBuilder MethodCallMessageBuilder = new();

		internal MethodResultMessage Invoke(GoreRequestMessage req, Func<GoreResponseMessage, Func<GoreRequestMessage, Task>, Task<MethodResultMessage?>> reponseHandler, CallOptions callOpt)
		{
			if (req.RequestType != RequestType.MethodCall)
				throw new Exception("RequestType is not RequestType.MethodCall");

			using (var call = _callInvoker.AsyncDuplexStreamingCall(DuplexCallDescriptor, null, callOpt))
			{
				try
				{
					call.RequestStream.WriteAsync(req).GetAwaiter().GetResult();
					while (call.ResponseStream.MoveNext().GetAwaiter().GetResult())
					{
						var resultMsg = reponseHandler(call.ResponseStream.Current, call.RequestStream.WriteAsync).GetAwaiter().GetResult();
						if (resultMsg != null)
							return resultMsg;
					}
					throw new Exception("No result message");
				}
				finally
				{
					call.RequestStream.CompleteAsync().GetAwaiter().GetResult();
				}
			}
		}

		internal async Task<MethodResultMessage> InvokeAsync(GoreRequestMessage req, Func<GoreResponseMessage, Func<GoreRequestMessage, Task>, Task<MethodResultMessage?>> reponseHandler, CallOptions callOpt)
		{
			if (req.RequestType != RequestType.MethodCall)
				throw new Exception("not RequestType.MethodCall");

			using (var call = _callInvoker.AsyncDuplexStreamingCall(DuplexCallDescriptor, null, callOpt))
			{
				try
				{
					await call.RequestStream.WriteAsync(req).ConfigureAwait(false);
					while (await call.ResponseStream.MoveNext().ConfigureAwait(false))
					{
						var resultMsg = await reponseHandler(call.ResponseStream.Current, call.RequestStream.WriteAsync).ConfigureAwait(false);
						if (resultMsg != null)
							return resultMsg;
					}
					throw new Exception("No result message");
				}
				finally
				{
					await call.RequestStream.CompleteAsync().ConfigureAwait(false);
				}
			}
		}

		public event EventHandler<Exception>? OneWayException;

		internal void OnOneWayException(Exception ex)
		{
			OneWayException?.Invoke(this, ex);
		}


	}

#if false
    /// <summary>
    /// Extension methods that simplify work with gRPC streaming calls.
    /// https://chromium.googlesource.com/external/github.com/grpc/grpc/+/chromium-deps/2016-07-19/src/csharp/Grpc.Core/Utils/AsyncStreamExtensions.cs
    /// </summary>
    static class AsyncStreamExtensions2
    {
        /// <summary>
        /// Reads the entire stream and executes an async action for each element.
        /// </summary>
        public static async Task ForEachAsync<T>(this IAsyncStreamReader<T> streamReader, Func<T, Task> asyncAction)
            where T : class
        {
            while (await streamReader.MoveNext().ConfigureAwait(false))
            {
                await asyncAction(streamReader.Current).ConfigureAwait(false);
            }
        }
    }
#endif


	public interface IRemotingParty
	{
		 ConcurrentDictionary<(MethodInfo, MessageType, int), Type[]> TypesCache { get; }
	}

}
