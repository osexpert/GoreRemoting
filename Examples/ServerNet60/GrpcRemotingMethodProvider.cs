﻿using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using GoreRemoting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            context.AddDuplexStreamingMethod(GoreRemoting.Descriptors.DuplexCall, new List<object>(), DuplexCall);
        }

        Task DuplexCall(GoreRemotingService service, IAsyncStreamReader<byte[]> input, IServerStreamWriter<byte[]> output, ServerCallContext serverCallContext)
            => pServ.DuplexCall(input, output, serverCallContext);
    }
}
