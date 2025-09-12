using System;
using System.Threading.Tasks;
using Grpc.Core;
using Channel = Grpc.Core.Channel;

namespace GoreRemoting.Tests.Tools;

public class NativeClient : RemotingClient, IAsyncDisposable
{
	Channel _channel;

	public NativeClient(int port, ClientConfig config) : base(GetInvoker(port, out var channel), config)
	{
		_channel = channel;
	}

	private static CallInvoker GetInvoker(int port, out Channel channel)
	{
		channel = new Channel("localhost", port, ChannelCredentials.Insecure);
		return channel.CreateCallInvoker();
	}

	public ValueTask DisposeAsync()
	{
		if (_channel != null)
			return new ValueTask(_channel.ShutdownAsync());
		else
			return default;
	}
}
