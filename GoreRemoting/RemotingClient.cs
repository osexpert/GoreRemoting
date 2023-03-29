using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;

namespace GoreRemoting
{

    public class RemotingClient
	{
        internal ClientConfig _config;
        CallInvoker _callInvoker;

        public RemotingClient(CallInvoker callInvoker, ClientConfig config)
		{
			_config = config;
            _callInvoker = callInvoker;
        }

        private static readonly Castle.DynamicProxy.ProxyGenerator ProxyGenerator = new Castle.DynamicProxy.ProxyGenerator();

        public T CreateProxy<T>()
        {
            var serviceProxyType = typeof(ServiceProxy<>).MakeGenericType(typeof(T));
            var serviceProxy = Activator.CreateInstance(serviceProxyType, this /* RemotingClient */);

            var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget(
                interfaceToProxy: typeof(T),
                interceptor: (Castle.DynamicProxy.IInterceptor)serviceProxy);

            return (T)proxy;
        }

        internal void BeforeMethodCall(Type serviceType, MethodInfo serviceMethod, Metadata headers, ref ISerializerAdapter serializer) => 
            _config.BeforeMethodCall?.Invoke(serviceType, serviceMethod, headers, ref serializer);

		public MethodCallMessageBuilder MethodCallMessageBuilder = new();


		internal MethodResultMessage Invoke(byte[] req, Func<byte[], Func<byte[], Task>, Task<MethodResultMessage>> reponseHandler, CallOptions callOpt)
        {
			using (var call = _callInvoker.AsyncDuplexStreamingCall(GoreRemoting.Descriptors.DuplexCall, null, callOpt))
            {
                try
                {
					call.RequestStream.WriteAsync(req).GetAwaiter().GetResult();
					while (call.ResponseStream.MoveNext().GetAwaiter().GetResult())
                    {
                        var resultMsg = reponseHandler(call.ResponseStream.Current, bytes => call.RequestStream.WriteAsync(bytes)).GetAwaiter().GetResult();
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

		internal async Task<MethodResultMessage> InvokeAsync(byte[] req, Func<byte[], Func<byte[], Task>, Task<MethodResultMessage>> reponseHandler, CallOptions callOpt)
		{
			using (var call = _callInvoker.AsyncDuplexStreamingCall(GoreRemoting.Descriptors.DuplexCall, null, callOpt))
			{
                try
                {
					await call.RequestStream.WriteAsync(req).ConfigureAwait(false);
					while (await call.ResponseStream.MoveNext().ConfigureAwait(false))
                    {
                        var resultMsg = await reponseHandler(call.ResponseStream.Current, bytes => call.RequestStream.WriteAsync(bytes)).ConfigureAwait(false);
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

        public event EventHandler<Exception> OneWayException;
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

  
}
