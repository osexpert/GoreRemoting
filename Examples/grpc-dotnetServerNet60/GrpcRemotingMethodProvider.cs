using GoreRemoting;
using GoreRemoting.RpcMessaging;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;

namespace grpcdotnetServerNet60;

internal class GoreRemotingMethodProvider : IServiceMethodProvider<GoreRemotingService>
{
	RemotingServer _server;

	public GoreRemotingMethodProvider(RemotingServer server)
	{
		_server = server;
	}

	public void OnServiceMethodDiscovery(ServiceMethodProviderContext<GoreRemotingService> context)
	{
		context.AddDuplexStreamingMethod(_server.DuplexCallDescriptor, new List<object>(), DuplexCall);
	}

	Task DuplexCall(GoreRemotingService service, IAsyncStreamReader<GoreRequestMessage> input, IServerStreamWriter<GoreResponseMessage> output, ServerCallContext serverCallContext)
		=> _server.DuplexCall(input, output, serverCallContext);
}
