using GoreRemoting;
using System.Threading.Tasks;
using System;
using Grpc.Core;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using ServerShared;
using GoreRemoting.Serialization.BinaryFormatter;
using System.Reflection;

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


		public object CreateInstance(GetServiceArgs a)
		{
			//Guid sessID = (Guid)CallContext.GetData("SessionId");
			Guid sessID = Guid.Parse(a.GrpcContext.RequestHeaders.GetValue(Constants.SessionIdHeaderKey));

			Console.WriteLine("SessID: " + sessID);

			return Activator.CreateInstance(a.ServiceType, sessID);
		}
	}


}