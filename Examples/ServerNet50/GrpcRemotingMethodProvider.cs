using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using GoreRemoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoreRemoting.RpcMessaging;

namespace ServerNet60
{
    internal class GoreRemotingMethodProvider : IServiceMethodProvider<GoreRemotingService>
    {
        RemotingServer pServ;

        public GoreRemotingMethodProvider(RemotingServer server)
        {
            pServ = server;
        }

        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GoreRemotingService> context)
        {
            context.AddDuplexStreamingMethod(pServ.DuplexCallDescriptor, new List<object>(), RpcCallBinaryFormatter);
        }

        Task RpcCallBinaryFormatter(GoreRemotingService service, IAsyncStreamReader<GoreRequestMessage> input, IServerStreamWriter<GoreResponseMessage> output, ServerCallContext serverCallContext)
            => pServ.DuplexCall(input, output, serverCallContext);

    }
}
