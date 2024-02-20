using System;
using ClientShared;
using GoreRemoting;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Core;

namespace ClientNet48
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var p = new Program();
			p.Go();
		}


		public void Go()
		{
			var channel = new Channel("localhost", 5000, ChannelCredentials.Insecure);
			var c = new RemotingClient(channel.CreateCallInvoker(), new ClientConfig(new BinaryFormatterAdapter())
			{
				BeforeCall = BeforeBuildMethodCallMessage,
			});
			var testServ = c.CreateProxy<ITestService>();

			var cs = new ClientTest();
			cs.Test(testServ);
		}

		Guid pSessID = Guid.NewGuid();

		public void BeforeBuildMethodCallMessage(BeforeCallArgs p)
		{
			//CallContext.SetData("SessionId", pSessID);
			p.Headers.Add(Constants.SessionIdHeaderKey, pSessID.ToString());
		}
	}



}
