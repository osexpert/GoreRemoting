using System;
using System.Collections.Generic;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Core;
using ServerShared;

namespace ServerNet48
{
	internal class Program
	{

		/// <param name="args"></param>
		/// <returns></returns>
		static void Main(string[] args)
		{
			Console.WriteLine("ServerNet48 example");

			var p = new Program();
			p.Go();
		}

		void Go()
		{
			var remServer = new RemotingServer(new ServerConfig(new BinaryFormatterAdapter())
			{
				GetService = CreateInstance,
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

			Console.WriteLine("running");

			// wait for shutdown
			server.ShutdownTask.GetAwaiter().GetResult();
		}


		public object CreateInstance(Type serviceType, ServerCallContext context)
		{
			//Guid sessID = (Guid)CallContext.GetData("SessionId");
			Guid sessID = Guid.Parse(context.RequestHeaders.GetValue(Constants.SessionIdHeaderKey));

			Console.WriteLine("SessID: " + sessID);

			return Activator.CreateInstance(serviceType, sessID);
		}
	}


}