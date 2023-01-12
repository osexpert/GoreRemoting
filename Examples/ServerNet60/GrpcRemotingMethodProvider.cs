using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using GrpcRemoting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerNet60
{
    internal class GrpcRemotingMethodProvider : IServiceMethodProvider<GrpcRemotingService>
    {
        RemotingServer pServ;

        public GrpcRemotingMethodProvider(RemotingServer server)
        {
            pServ = server;
        }

        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GrpcRemotingService> context)
        {
            context.AddDuplexStreamingMethod(GrpcRemoting.Descriptors.DuplexCall, new List<object>(), DuplexCall);
        }

        Task DuplexCall(GrpcRemotingService service, IAsyncStreamReader<byte[]> input, IServerStreamWriter<byte[]> output, ServerCallContext serverCallContext)
            => pServ.DuplexCall(input, output, serverCallContext);
    }
}
