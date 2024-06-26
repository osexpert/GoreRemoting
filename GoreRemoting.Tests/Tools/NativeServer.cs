﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace GoreRemoting.Tests.Tools
{
	public class NativeServer : RemotingServer, IAsyncDisposable
	{

		Grpc.Core.Server _server;

		public NativeServer(int port, ServerConfig config) : base(config)
		{
			var options = new List<ChannelOption>();
			options.Add(new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue));
			options.Add(new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue));

			_server = new Grpc.Core.Server(options)
			{
				Services =
				{
					ServerServiceDefinition.CreateBuilder()
						.AddMethod(DuplexCallDescriptor, this.DuplexCall)
						.Build()
				}
			};

			_server.Ports.Add("0.0.0.0", port, ServerCredentials.Insecure);
		}

		public ValueTask DisposeAsync()
		{
			if (_server != null)
				return new ValueTask(_server.ShutdownAsync());
			else
				return default;
		}

		public void Start() => _server.Start();
	}
}
