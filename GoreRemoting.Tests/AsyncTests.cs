using GoreRemoting.Serialization;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Serialization.Json;
using GoreRemoting.Serialization.MemoryPack;
using GoreRemoting.Serialization.MessagePack;
using GoreRemoting.Tests.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace GoreRemoting.Tests
{
    public class AsyncTests
    {
        #region Service with async method

        public interface IAsyncService
        {
            Task<string> ConvertToBase64Async(string text);

            Task NonGenericTask();

            (Version, Version, Version[], Version[]) TestMisc(Version v, Version v2, Version[] versions, Version[] versions2);
        }

        public class AsyncService : IAsyncService
        {
            public async Task<string> ConvertToBase64Async(string text)
            {
                var convertFunc = new Func<string>(() =>
                {
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
                    return Convert.ToBase64String(stream.ToArray());
                });

                var base64String = await Task.Run(convertFunc);

                return base64String;
            }

            public Task NonGenericTask()
            {
                return Task.CompletedTask;
            }

			public (Version, Version, Version[], Version[]) TestMisc(Version v, Version vs, Version[] versions, Version[] versions2)
			{
				return (v, vs, versions, versions2);
			}
		}

		#endregion



		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.MessagePack)]
		public async void AsyncMethods_should_work(enSerializer ser)
		{
			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser))
				{
					//RegisterServicesAction = container =>
					//    container.RegisterService<IAsyncService, AsyncService>(
					//        lifetime: ServiceLifetime.Singleton)
				};

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IAsyncService, AsyncService>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IAsyncService>();

			var base64String = await proxy.ConvertToBase64Async("Yay");

			Assert.Equal("WWF5", base64String);

			var res = proxy.TestMisc(null, new Version(1, 42), new[] { new Version(1, 2), new Version(2, 3, 5) }, null);
			Assert.Null(res.Item1);
			Assert.Null(res.Item4);
			Assert.Equal(new Version(1, 42), res.Item2);
			Assert.Equal(2, res.Item3.Length);
			Assert.Equal(new Version(1, 2), res.Item3[0]);
			Assert.Equal(new Version(2, 3, 5), res.Item3[1]);
		}

		/// <summary>
		/// Awaiting for ordinary non-generic task method should not hangs. 
		/// </summary>
		//[Fact(Timeout = 15000)]
		[Theory(Timeout = 15000)]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task AwaitingNonGenericTask_should_not_hang_forever(enSerializer ser)
        {
            var port = 9197;
            
            var serverConfig =
                new ServerConfig(Serializers.GetSerializer(ser))
                {
					//RegisterServicesAction = container =>
					//    container.RegisterService<IAsyncService, AsyncService>(
					//        lifetime: ServiceLifetime.Singleton)
				};

			await using var server = new NativeServer(port, serverConfig);
			server.RegisterService<IAsyncService, AsyncService>();
			server.Start();

            await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

            var proxy = client.CreateProxy<IAsyncService>();

            await proxy.NonGenericTask();
        }
    }

	public enum enSerializer
	{
		BinaryFormatter,
		MemoryPack,
        Json,
        MessagePack

	}

    public static class Serializers
    {
		public static ISerializerAdapter GetSerializer(enSerializer ser)
		{
			return ser switch
			{
				enSerializer.BinaryFormatter => new BinaryFormatterAdapter(),
				enSerializer.MemoryPack => new MemoryPackAdapter(),
				enSerializer.Json => new JsonAdapter(),
				enSerializer.MessagePack => new MessagePackAdapter(),
				_ => throw new NotImplementedException(),
			};
		}
	}
}
