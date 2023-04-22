using ClientShared;
using Grpc.Core;
using GoreRemoting;
using GoreRemoting.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using GoreRemoting.Serialization.BinaryFormatter;
using Grpc.Net.Compression;

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
                BeforeMethodCall = BeforeBuildMethodCallMessage, 
            });
            var testServ = c.CreateProxy<ITestService>();

            var cs = new ClientTest();
            cs.Test(testServ);
        }

        Guid pSessID = Guid.NewGuid();

        public void BeforeBuildMethodCallMessage(BeforeMethodCallParams p)
        {
			//CallContext.SetData("SessionId", pSessID);
			p.Headers.Add(Constants.SessionIdHeaderKey, pSessID.ToString());
		}
    }

 

}
