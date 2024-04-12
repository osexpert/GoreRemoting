using ClientShared;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Net.Client;

namespace grpcdotnetClientNet60
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("ClientNet60 example");

			var p = new Program();
			p.Go();
		}


		public void Go()
		{
			var channel = GrpcChannel.ForAddress("http://localhost:5000");

			var c = new RemotingClient(channel.CreateCallInvoker(), new ClientConfig(new BinaryFormatterAdapter())
			{
				BeforeCall = BeforeBuildMethodCallMessage,
			});

			var testServ = c.CreateProxy<ITestService>();

			var cs = new ClientTest();
			cs.Test(testServ);

			//while (true)
			//{
			//	try
			//	{
			//		var k = testServ.Echo("lol");
			//		var ff = testServ.EchoAsync("ff").GetAwaiter().GetResult();
			//	}
			//	catch { }
			//}

		}

		Guid pSessID = Guid.NewGuid();

		public void BeforeBuildMethodCallMessage(BeforeCallArgs p)
		{
			p.Headers.Add(Constants.SessionIdHeaderKey, pSessID.ToString());
			//CallContext.SetData("SessionId", pSessID);
		}

	}


}
