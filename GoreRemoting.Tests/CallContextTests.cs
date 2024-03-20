using System.Threading;
using System.Threading.Tasks;
using GoreRemoting.Tests.Tools;
using Xunit;

namespace GoreRemoting.Tests
{
	public class CallContextTests
	{
		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		[InlineData(enSerializer.Protobuf)]
		public async Task CallContext_should_flow_from_client_to_server_and_back(enSerializer ser)
		{
			var testService =
				new TestService
				{
					TestMethodFake = _ =>
					{
						// I don't think it is possible to test this in same process...
						CallContext.SetData("test", "Changed");
						CallContext.SetData("test2", null);
						return (string)CallContext.GetData("test")!;
					}
				};

			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser))
				{
					//RegisterServicesAction = container =>
					//    container.RegisterService<ITestService>(
					//        factoryDelegate: () => testService,
					//        lifetime: ServiceLifetime.Singleton)
					GetService = (_, _) => testService
				};

			await using var server = new NativeServer(9093, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			var clientThread =
				new Thread(async () =>
				{
					CallContext.SetData("test", "CallContext");
					Assert.Equal("CallContext", CallContext.GetData("test"));

					await using var client = new NativeClient(9093, new ClientConfig(Serializers.GetSerializer(ser)));

					var localCallContextValueBeforeRpc = CallContext.GetData("test");

					var proxy = client.CreateProxy<ITestService>();
					var result = (string)proxy.TestMethod("x")!;

					var localCallContextValueAfterRpc = CallContext.GetData("test");

					Assert.NotEqual(localCallContextValueBeforeRpc, result);
					Assert.Equal("Changed", result);
					Assert.Equal("Changed", CallContext.GetData("test"));
					Assert.Equal("Changed", localCallContextValueAfterRpc);
				});

			clientThread.Start();
			clientThread.Join();
		}
	}
}
