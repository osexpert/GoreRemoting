using ClientShared;
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
            
            var c = new RemotingClient(channel.CreateCallInvoker(), new ClientConfig 
            { 
                BeforeMethodCall = BeforeBuildMethodCallMessage
            });

            var testServ = c.CreateProxy<ITestService>();

            var cs = new ClientTest();
            cs.Test(testServ);
        }

        Guid pSessID = Guid.NewGuid();

        public void BeforeBuildMethodCallMessage(Type t, MethodInfo mi, Metadata headers, ref ISerializerAdapter serializer)
        {
			// check method...
            var a1 = mi.GetCustomAttribute<MemoryPackSerializerAttribute>();
            if (a1 != null)
            {
				serializer = new mempack();
			}
            else
            {
				// ...then service itself
                var t1 = t.GetCustomAttribute<MemoryPackSerializerAttribute>();
                if (t1 != null)
                    serializer = new mempack();
            }

			headers.Add(Constants.SessionIdHeaderKey, pSessID.ToString());
			//CallContext.SetData("SessionId", pSessID);
        }

    }

	class mempack : ISerializerAdapter
	{
		public string Name => throw new NotImplementedException();

		public T Deserialize<T>(byte[] rawData)
		{
			throw new NotImplementedException();
		}

		public T Deserialize<T>(Stream rawData)
		{
			throw new NotImplementedException();
		}

		public object Deserialize(Type type, byte[] rawData)
		{
			throw new NotImplementedException();
		}

		public Exception GetException(Exception ex2)
		{
			throw new NotImplementedException();
		}

		public byte[] Serialize<T>(T graph)
		{
			throw new NotImplementedException();
		}

		public byte[] Serialize(Type type, object graph)
		{
			throw new NotImplementedException();
		}

		public void Serialize<T>(Stream s, T graph)
		{
			throw new NotImplementedException();
		}
	}

	//public class SerializerAttribute : Attribute
	//{
	//	public string Name { get; private set; }

	//	public SerializerAttribute(string name)
	//	{
	//		Name = name;
	//	}
	//}


}
