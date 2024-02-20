using GoreRemoting.Serialization;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Serialization.Json;
using GoreRemoting.Serialization.MemoryPack;
using GoreRemoting.Serialization.MessagePack;
using GoreRemoting.Tests.Tools;
using Grpc.Core;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using static GoreRemoting.Tests.AsyncTests;

namespace GoreRemoting.Tests
{
	public class AsyncTests
	{
		#region Service with async method

		public interface IAsyncService
		{
			Task<string> ConvertToBase64Async(string text);

			Task NonGenericTask();

			(Version?, Version, Version[], Version[]?) TestMisc(Version? v, Version v2, Version[] versions, Version[]? versions2);
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

			public (Version?, Version, Version[], Version[]?) TestMisc(Version? v, Version vs, Version[] versions, Version[]? versions2)
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

		public interface IExceptionTest
		{
			void TestSerializedExMistakeAndPrivate();
			void TestSerializedExMistake();
			void TestSerializedOk();

			void TestWithInnerException();
		}

		class ExceptionTest : IExceptionTest
		{
			public void TestSerializedExMistake()
			{
				throw new SerExMistakeNotPriv("The mess", "extra string");
			}

			public void TestSerializedExMistakeAndPrivate()
			{
				throw new SerExMistake("The mess", "extra string");
			}

			public void TestSerializedOk()
			{
				throw new SerExOk("The mess", "extra string");
			}

			public void TestWithInnerException()
			{
				Exception? ie = null;
				try
				{
					var c = new SqlConnection("lolx");
					c.Open();
					c.BeginTransaction();
				}
				catch (Exception e)
				{
					ie = e;
				}

				throw new SerExOk("The mess", "extra string", ie!);
			}
		}

		/// <summary>
		/// json can not find the type (private?). fails over to Exception..
		/// same with messpack? fails over to Exception..
		/// also binaryformatter withh throw bcause cant find property "Test"
		/// 
		/// the failover type here is not stable\consistent...
		/// 
		/// i would have liked:
		/// TheRealException
		/// SomeRemoteException with type and stack and serinfo in it
		/// SomeRemoteException with type and stack (assuming if serinfo fails to desser)
		/// 
		/// Mao: only 1 other exception to care about
		/// </summary>
		[Serializable]
		class SerExMistake : Exception
		{
			public string Test { get; }

			public SerExMistake(string theMess, string t) : base(theMess)
			{
				Test = t;
				Data.Add("teste", "foobar");
			}

			public SerExMistake(SerializationInfo si, StreamingContext sc) : base(si, sc)
			{
				// Mistake: should be "test"
				Test = si.GetString("Test")!;
			}

			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);
				info.AddValue("test", Test);
			}
		}

		[Serializable]
		public class SerExMistakeNotPriv : Exception
		{
			public string Test { get; }

			public SerExMistakeNotPriv(string theMess, string t) : base(theMess)
			{
				Test = t;
				Data.Add("teste", "foobar");
			}

			public SerExMistakeNotPriv(SerializationInfo si, StreamingContext sc) : base(si, sc)
			{
				// Mistake: should be "test"
				Test = si.GetString("Test")!;
			}

			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);
				info.AddValue("test", Test);
			}
		}

		[Serializable]
		public class SerExOk : Exception
		{
			public string Test { get; }

			public SerExOk(string theMess, string t) : base(theMess)
			{
				Test = t;
				Data.Add("teste", "foobar");
			}

			public SerExOk(string theMess, string t, Exception ie) : base(theMess, ie)
			{
				Test = t;
				Data.Add("teste", "foobar");
			}


			public SerExOk(SerializationInfo si, StreamingContext sc) : base(si, sc)
			{
				Test = si.GetString("test")!;
			}

			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);
				info.AddValue("test", Test);
			}
		}


		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.MessagePack)]
		public async void ExceptionTests(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IExceptionTest, ExceptionTest>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IExceptionTest>();

			Exception? e1 = null;
			try
			{
				proxy.TestSerializedExMistakeAndPrivate();
			}
			catch (Exception e)
			{
				e1 = e;
			}

			//Assert.IsType<SerEx>(e1);
			//Assert.Equal("The mess", e1.Message);

			var lines = e1!.ToString().Split(Environment.NewLine).Length;
			//Assert.Equal(9, lines);
			if (ser == enSerializer.BinaryFormatter)
			{
				//	Assert.Equal(28, lines); // a failure to deserialize?? yes
				Assert.Equal(8, lines); // task was cancelled, due too no result message
			}
			else
				Assert.Equal(26, lines);

			// Most will fail because SerExMistake is private

			if (ser == enSerializer.BinaryFormatter)
			{
				Assert.IsType<TaskCanceledException>(e1);
			}
			else
			{
				// because it can't find "Test"?
				Assert.IsType<SerializationException>(e1);
				//Assert.IsType<SerExMistake>(e1);
				//Assert.Equal(1, e1.Data.Count);
			}

			//Assert.Equal("test", e1.Test);

			Exception? e2 = null;
			try
			{
				proxy.TestSerializedExMistake();
			}
			catch (Exception e)
			{
				e2 = e;
			}

			var lines2 = e2!.ToString().Split(Environment.NewLine).Length;

			//Assert.Equal(9, lines2);

			if (ser == enSerializer.BinaryFormatter)
			{
				//	Assert.Equal(28, lines2); // failure to desser
				Assert.Equal(8, lines2); // failure to desser
			}
			else
				Assert.Equal(26, lines2);

			// Most will fail because SerExMistake is private

			if (ser == enSerializer.BinaryFormatter)
			{
				Assert.IsType<TaskCanceledException>(e2);
			}
			else
			{

				// because it can't find "Test"?
				Assert.IsType<SerializationException>(e2);
				//	Assert.IsType<SerExMistakeNotPriv>(e2);
				//	Assert.Equal(1, e2.Data.Count);
			}

			Exception? e3 = null;
			try
			{
				proxy.TestSerializedOk();
			}
			catch (Exception e)
			{
				e3 = e;
			}

			var lines3 = e3!.ToString().Split(Environment.NewLine).Length;
			//if (ser == enSerializer.BinaryFormatter)
			//	Assert.Equal(28, lines3);
			//else
			{
				Assert.Equal(9, lines3);
				//Assert.Equal("GoreRemoting.Tests.AsyncTests+SerExOk, GoreRemoting.Tests", ((SerExOk)e3).TypeName);
			}

			// Most will fail because SerExMistake is private

			// because it can't find "Test"?
			Assert.IsType<SerExOk>(e3);

			//			Assert.Equal("tull", e3.Data["teste"]);
			//Assert.Equal(1, e3.Data.Count);
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.MessagePack)]
		public async void ExceptionTests_InnerEx(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IExceptionTest, ExceptionTest>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IExceptionTest>();




			Exception? e4 = null;
			try
			{
				proxy.TestWithInnerException();
			}
			catch (Exception e)
			{
				e4 = e;
			}

			var lines4 = e4!.ToString().Split(Environment.NewLine).Length;
			//if (ser == enSerializer.BinaryFormatter)
			//	Assert.Equal(28, lines3);
			//else
			{
				Assert.Equal(21, lines4);
				//Assert.Equal("GoreRemoting.Tests.AsyncTests+SerExOk, GoreRemoting.Tests", ((SerExOk)e3).TypeName);
			}

			// Most will fail because SerExMistake is private

			// because it can't find "Test"?
			Assert.IsType<SerExOk>(e4);

			//		Assert.Equal("tull", e4.Data["teste"]);
			Assert.Single(e4.Data);
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