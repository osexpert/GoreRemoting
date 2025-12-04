using System.Threading;
using System.Threading.Tasks;
using GoreRemoting.Tests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoreRemoting.Tests;

[TestClass]
public class CallContextTests
{
	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task CallContext_should_flow_from_client_to_server_and_back(Serializer ser)
	{
		var testService =
			new TestService
			{
				TestMethodFake = _ =>
				{
					// I don't think it is possible to test this in same process...
					CallContext.SetValue("test", "Changed");
					CallContext.SetValue<string>("test2", null);
					return CallContext.GetValue<string>("test")!;
				}
			};

		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser))
			{
				CreateService = (_, _) => new(testService, false)
			};

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		Exception? ex = null;

		var clientThread =
			new Thread(async () =>
			{
#pragma warning disable MSTEST0040
				try
				{
					var g = Guid.NewGuid();
					CallContext.SetValue("testGuid", g);
					var t = DateTime.Now;
					CallContext.SetValue("testTime", t);
					CallContext.SetValue("test", "CallContext");
					Assert.AreEqual("CallContext", CallContext.GetValue<string>("test"));

					await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

					var localCallContextValueBeforeRpc = CallContext.GetValue<string>("test");

					var proxy = client.CreateProxy<ITestService>();
					var result = (string)proxy.TestMethod("x")!;

					var localCallContextValueAfterRpc = CallContext.GetValue<string>("test");

					Assert.AreNotEqual(localCallContextValueBeforeRpc, result);
					Assert.AreEqual("Changed", result);
					Assert.AreEqual(g, CallContext.GetValue<Guid>("testGuid"));
					Assert.AreEqual(t, CallContext.GetValue<DateTime>("testTime"));
					Assert.AreEqual("Changed", CallContext.GetValue<string>("test"));
					Assert.AreEqual("Changed", localCallContextValueAfterRpc);
				}
				catch (Exception e)
				{
					ex = e;
				}
#pragma warning restore MSTEST0040
			});

		clientThread.Start();
		clientThread.Join();

		if (ex != null)
			Assert.Fail();
	}
}
