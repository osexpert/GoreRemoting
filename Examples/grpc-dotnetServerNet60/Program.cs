using System.Collections.Concurrent;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServerShared;

namespace grpcdotnetServerNet60
{
	internal class Program
	{
		/// <summary>
		/// grpc-dotnet/perf/benchmarkapps/QpsWorker
		/// https://github.com/grpc/grpc-dotnet/pull/1617/files#diff-4cde0178bebee2be11d6a73b69dfaffe4156abd06952f82072d376bea5dcecd0
		/// Found here: https://github.com/grpc/grpc-dotnet/issues/1628#issuecomment-1063465524
		/// 
		/// Somthing about dynamic services: https://github.com/grpc/grpc-dotnet/issues/1690
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			Console.WriteLine("ServerNet60 example");

			var p = new Program();

			var server = new RemotingServer(new ServerConfig(new BinaryFormatterAdapter())
			{
				CreateService = p.CreateInstance,
			});

			server.RegisterService<ITestService, TestService>();
			server.RegisterService<IOtherService, OtherService>();

			var builder = WebApplication.CreateBuilder(args);

			var services = builder.Services;

			services.AddGrpc(o =>
			{
				// Small performance benefit to not add catch-all routes to handle UNIMPLEMENTED for unknown services
				o.IgnoreUnknownServices = true;
				//o.MaxSendMessageSize
				//o.MaxReceiveMessageSize
			});
			services.Configure<RouteOptions>(c =>
			{
				// Small performance benefit to skip checking for security metadata on endpoint
				c.SuppressCheckForUnhandledSecurityMetadata = true;
			});

			services.AddSingleton<IDITest>(new DITest());

			services.AddSingleton<GoreRemotingService>();
			services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<GoreRemotingService>), new GoreRemotingMethodProvider(server)));

			builder.WebHost.ConfigureKestrel(kestrel =>
			{
				//              var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
				//                var certPath = Path.Combine(basePath!, "Certs", "server1.pfx");

				kestrel.ListenAnyIP(5000, listenOptions =>
				{
					listenOptions.Protocols = HttpProtocols.Http2;

					// Contents of "securityParams" are basically ignored.
					// Instead the server is setup with the default test cert.
					//if (config.SecurityParams != null)
					//{
					//    listenOptions.UseHttps(certPath, "1111");
					//}
				}
				);

				// Other gRPC servers don't include a server header
				kestrel.AddServerHeader = false;
			});

			builder.Logging.ClearProviders();

			var app = builder.Build();

			app.MapGrpcService<GoreRemotingService>();
			app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

			var task = app.RunAsync();

			Console.WriteLine("Server running");

			await task;// Task.Delay(-1);
		}

		//public ServiceHandle CreateInstance(Type serviceType, ServerCallContext context)
		//{
		//	//Guid sessID = (Guid)CallContext.GetData("SessionId");
		//	Guid sessID = Guid.Parse(context.RequestHeaders.GetValue(Constants.SessionIdHeaderKey)!);

		//	Console.WriteLine("SessID: " + sessID);

		//	// TODO: make the service work with DI
		//	return new(Activator.CreateInstance(serviceType, sessID) ?? throw new Exception("Can't create instance: " + serviceType), true);
		//}

		//private static readonly Lazy<ObjectFactory> _objectFactory = new Lazy<ObjectFactory>(static ()
		//	=> ActivatorUtilities.CreateFactory(typeof(GoreRemotingService), new Type[] { typeof(Guid) }));

		ConcurrentDictionary<Type, ObjectFactory> _factories = new();

		ServiceHandle CreateInstance(Type serviceType, ServerCallContext context)
		{
			//Guid sessID = (Guid)CallContext.GetData("SessionId");
			Guid sessionId = Guid.Parse(context.RequestHeaders.GetValue(Constants.SessionIdHeaderKey)!);

			Console.WriteLine("SessionId: " + sessionId);

			var factory = _factories.GetOrAdd(serviceType, st => ActivatorUtilities.CreateFactory(st, new Type[] { typeof(Guid) }));

			var service = factory(context.GetHttpContext().RequestServices, new object?[] { sessionId });// Array.Empty<object>());
			return new(service, true);

			//return service;
			//		return Activator.CreateInstance(serviceType, sessID) ?? throw new Exception("Can't create instance: " + serviceType);
		}
	}

}
