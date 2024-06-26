﻿using System;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ServerShared;

namespace grpcdotnetServerNet50
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{

			//services.AddGrpc();

			services.AddGrpc(o =>
			{
				// Small performance benefit to not add catch-all routes to handle UNIMPLEMENTED for unknown services
				//o.IgnoreUnknownServices = true;
				//o.MaxSendMessageSize
				//o.MaxReceiveMessageSize
			});
			//services.Configure<RouteOptions>(c =>
			//{
			//	// Small performance benefit to skip checking for security metadata on endpoint
			//	//c.SuppressCheckForUnhandledSecurityMetadata = true;
			//});

			services.AddSingleton<GoreRemotingService>();

			var server = new RemotingServer(new ServerConfig(new BinaryFormatterAdapter()) { CreateService = CreateInstance });

			server.RegisterService<ITestService, TestService>();


			services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<GoreRemotingService>), new GoreRemotingMethodProvider(server)));
		}

		public ServiceHandle CreateInstance(Type serviceType, ServerCallContext context)
		{
			//Guid sessID = (Guid)CallContext.GetData("SessionId");
			Guid sessID = Guid.Parse(context.RequestHeaders.GetValue(Constants.SessionIdHeaderKey)!);

			Console.WriteLine("SessID: " + sessID);

			return new(Activator.CreateInstance(serviceType, sessID), true);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<GoreRemotingService>();
				endpoints.MapGet("/", async context => await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"));

				//endpoints.MapGrpcService<GreeterService>();

				//endpoints.MapGet("/", async context =>
				//{
				//	await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
				//});
			});
		}
	}
}
