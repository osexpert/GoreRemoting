using GoreRemoting.Serialization;
using GoreRemoting.Serialization.Binary;
using GoreRemoting.Serialization.Json;
using GoreRemoting.Serialization.MemoryPack;
using GoreRemoting.Tests.Tools;
using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;
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
        }

		#endregion



        [Theory]
        [InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		public async void AsyncMethods_should_work(enSerializer ser)
        {
            var serverConfig =
                new ServerConfig()
                {
					Serializer = GetSerializer(ser)
					//RegisterServicesAction = container =>
					//    container.RegisterService<IAsyncService, AsyncService>(
					//        lifetime: ServiceLifetime.Singleton)
				};

            await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IAsyncService, AsyncService>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig() { DefaultSerializer = GetSerializer(ser) });

            var proxy = client.CreateProxy<IAsyncService>();

            var base64String = await proxy.ConvertToBase64Async("Yay");

            Assert.Equal("WWF5", base64String);
        }

		private ISerializerAdapter GetSerializer(enSerializer ser)
		{
            return ser switch
            {
                enSerializer.BinaryFormatter => new BinarySerializerAdapter(),
                enSerializer.MemoryPack => new MemoryPackAdapter(),
                enSerializer.Json => new JsonAdapter(),
                _ => throw new NotImplementedException(),
			};
		}

		/// <summary>
		/// Awaiting for ordinary non-generic task method should not hangs. 
		/// </summary>
		//[Fact(Timeout = 15000)]
		[Theory(Timeout = 15000)]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		public async Task AwaitingNonGenericTask_should_not_hang_forever(enSerializer ser)
        {
            var port = 9197;
            
            var serverConfig =
                new ServerConfig()
                {
					Serializer = GetSerializer(ser)
					//RegisterServicesAction = container =>
					//    container.RegisterService<IAsyncService, AsyncService>(
					//        lifetime: ServiceLifetime.Singleton)
				};

			await using var server = new NativeServer(port, serverConfig);
			server.RegisterService<IAsyncService, AsyncService>();
			server.Start();

            await using var client = new NativeClient(port, new ClientConfig() { DefaultSerializer = GetSerializer(ser) });

            var proxy = client.CreateProxy<IAsyncService>();

            await proxy.NonGenericTask();
        }
    }

	public enum enSerializer
	{
		BinaryFormatter,
		MemoryPack,
        Json
	}
}
