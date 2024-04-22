using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Core;
using ServerShared;

namespace GrpcCoreServerNet60
{
	internal class Program
	{

		/// <param name="args"></param>
		/// <returns></returns>
		static Task Main(string[] args)
		{
			Console.WriteLine("NativeServerNet60 example");

			var p = new Program();
			var task = p.Go();

			Console.WriteLine("Server running");

			return task;
		}

		Task Go()
		{
			var remServer = new RemotingServer(new ServerConfig(new BinaryFormatterAdapter())
			{
				CreateService = CreateInstance,
			});
			remServer.RegisterService<ITestService, TestService>();

			var options = new List<ChannelOption>();
			options.Add(new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue));
			options.Add(new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue));

			var server = new Grpc.Core.Server(options)
			{
				Services =
				{
					ServerServiceDefinition.CreateBuilder()
						.AddMethod(remServer.DuplexCallDescriptor, remServer.DuplexCall)
						.Build()
				}
			};

			server.Ports.Add("0.0.0.0", 5000, ServerCredentials.Insecure);

			server.Start();

			// wait for shutdown
			return server.ShutdownTask;
		}


		public ServiceHandle CreateInstance(Type serviceType, ServerCallContext context)
		{
			//Guid sessID = (Guid)CallContext.GetData("SessionId");
			Guid sessID = Guid.Parse(context.RequestHeaders.GetValue(Constants.SessionIdHeaderKey));

			Console.WriteLine("SessID: " + sessID);

			return new(Activator.CreateInstance(serviceType, sessID), true);
		}
	}


}
