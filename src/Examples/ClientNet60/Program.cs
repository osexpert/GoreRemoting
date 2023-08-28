﻿using ClientShared;
using Grpc.Core;
using Grpc.Net.Client;
using GoreRemoting;
using GoreRemoting.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Net.Compression;

namespace ClientNet60
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
			var channel = GrpcChannel.ForAddress("http://localhost:5000");

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
			p.Headers.Add(Constants.SessionIdHeaderKey, pSessID.ToString());
			//CallContext.SetData("SessionId", pSessID);
		}

	}


}