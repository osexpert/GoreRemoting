using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using GoreRemoting.RpcMessaging;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static GoreRemoting.AspNetCore.Server.ServicesExtensions;

namespace GoreRemoting.AspNetCore.Server
{

	public static class ServicesExtensions
	{
		public static IGrpcServerBuilder AddGoreRemoting<TGrpcService>(this IServiceCollection services, Action<ServerConfig> goreConfigure, Action<GrpcServiceOptions>? grpcConfigure = null)
			where TGrpcService : class
		{
			var builder = grpcConfigure == null ? services.AddGrpc() : services.AddGrpc(grpcConfigure);
			var goreConf = new ServerConfig();
			goreConfigure(goreConf);

			var server = new RemotingServer<TGrpcService>(goreConf);
			services.AddSingleton(server);

			services.AddSingleton<TGrpcService>();
			services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<TGrpcService>), new GoreRemotingMethodProvider<TGrpcService>(server)));

			return builder;
		}

		class GoreRemotingMethodProvider<TGrpcService> : IServiceMethodProvider<TGrpcService>
			where TGrpcService : class
		{
			RemotingServer _server;

			public GoreRemotingMethodProvider(RemotingServer server)
			{
				_server = server;
			}

			public void OnServiceMethodDiscovery(ServiceMethodProviderContext<TGrpcService> context)
			{
				context.AddDuplexStreamingMethod(_server.DuplexCallDescriptor, new List<object>(), DuplexCall);
			}

			Task DuplexCall(TGrpcService service, // service not used
				IAsyncStreamReader<GoreRequestMessage> input, 
				IServerStreamWriter<GoreResponseMessage> output,
				ServerCallContext serverCallContext) => _server.DuplexCall(input, output, serverCallContext);
		}

		public static GrpcServiceEndpointConventionBuilder MapGoreRemotingServices<TGrpcService>(this IEndpointRouteBuilder builder,
			Action<RemotingServer> addServices)
			where TGrpcService : class
		{
			var server = builder.ServiceProvider.GetRequiredService<RemotingServer<TGrpcService>>();
			addServices(server);
			return builder.MapGrpcService<TGrpcService>();
		}

	}

	[GrpcServiceName(Constants.GrpcServiceName)]
	public class GoreRemotingService
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class GrpcServiceNameAttribute : Attribute
	{
		public string Name { get; }
		public GrpcServiceNameAttribute(string name)
		{
			Name = name;
		}
	}

	public class RemotingServer<TGrpcService> : RemotingServer
		where TGrpcService : class
	{
		public RemotingServer(ServerConfig config) : base(config)
		{
			var st = typeof(TGrpcService);
			config.GrpcServiceName = st.GetCustomAttribute<GrpcServiceNameAttribute>()?.Name ?? st.Name;
		}
	}

}
