using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using GoreRemoting.Serialization;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Serialization.Json;
using GoreRemoting.Serialization.MemoryPack;
using GoreRemoting.Serialization.MessagePack;
using GoreRemoting.Serialization.Protobuf;
using GoreRemoting.Tests.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace GoreRemoting.Tests
{
	[TestClass]
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



		[TestMethod]
		[DataRow(enSerializer.BinaryFormatter)]
		[DataRow(enSerializer.Json)]
		[DataRow(enSerializer.MemoryPack)]
		[DataRow(enSerializer.MessagePack)]
		[DataRow(enSerializer.Protobuf)]
		public async Task AsyncMethods_should_work(enSerializer ser)
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

			Assert.AreEqual("WWF5", base64String);

			var res = proxy.TestMisc(null, new Version(1, 42), new[] { new Version(1, 2), new Version(2, 3, 5) }, null);
			Assert.IsNull(res.Item1);
			Assert.IsNull(res.Item4);
			Assert.AreEqual(new Version(1, 42), res.Item2);
			Assert.AreEqual(2, res.Item3.Length);
			Assert.AreEqual(new Version(1, 2), res.Item3[0]);
			Assert.AreEqual(new Version(2, 3, 5), res.Item3[1]);
		}

		/// <summary>
		/// Awaiting for ordinary non-generic task method should not hangs. 
		/// </summary>
		//[Fact(Timeout = 15000)]
		[TestMethod()]//Timeout = 15000)]
		[DataRow(enSerializer.BinaryFormatter)]
		[DataRow(enSerializer.MemoryPack)]
		[DataRow(enSerializer.Json)]
		[DataRow(enSerializer.MessagePack)]
		[DataRow(enSerializer.Protobuf)]
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
			void TestWithSqlException();
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

			public void TestWithSqlException()
			{
				using var c = new SqlConnection("Data Source=();Initial Catalog=test; Integrated Security=sspi;");
				c.Open();
				var cm = new SqlCommand("select * from test", c);
				cm.ExecuteNonQuery();
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


		[TestMethod]
		[DataRow(enSerializer.BinaryFormatter)]
		[DataRow(enSerializer.Json)]
		[DataRow(enSerializer.MemoryPack)]
		[DataRow(enSerializer.MessagePack)]
		[DataRow(enSerializer.Protobuf)]
		public async Task ExceptionTests(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IExceptionTest, ExceptionTest>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IExceptionTest>();


			Exception e;		
			if (ser == enSerializer.BinaryFormatter)
			{
				e = Assert.ThrowsException<TaskCanceledException>(proxy.TestSerializedExMistakeAndPrivate);
				Assert.AreEqual("A task was canceled.", e.Message);
				Assert.IsNull(e.InnerException);

				AssertLines(e, new string[]
				{
					"System.Threading.Tasks.TaskCanceledException: A task was canceled.",
					"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
				});
			}
			else
			{
				e = Assert.ThrowsException<TargetInvocationException>(proxy.TestSerializedExMistakeAndPrivate);
				Assert.AreEqual("Exception has been thrown by the target of an invocation.", e.Message);
				Assert.AreEqual("Member 'Test' was not found.", e.InnerException!.Message);

				AssertLines(e, new string[]
{
				"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.",
				" ---> System.Runtime.Serialization.SerializationException: Member 'Test' was not found.",
				"   --- End of inner exception stack trace ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
});			}

			Exception e2;
			if (ser == enSerializer.BinaryFormatter)
			{
				e2 = Assert.ThrowsException<TaskCanceledException>(proxy.TestSerializedExMistake);

				Assert.AreEqual("A task was canceled.", e2.Message);
				Assert.IsNull(e2.InnerException);

				AssertLines(e, new string[]{
					"System.Threading.Tasks.TaskCanceledException: A task was canceled.",
					"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
				});
			}
			else
			{
				e2 = Assert.ThrowsException<TargetInvocationException>(proxy.TestSerializedExMistake);
				Assert.AreEqual("Exception has been thrown by the target of an invocation.", e2.Message);
				Assert.AreEqual("Member 'Test' was not found.", e2.InnerException!.Message);

				AssertLines(e2, new string[]{
					"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.",
					" ---> System.Runtime.Serialization.SerializationException: Member 'Test' was not found.",
					"   --- End of inner exception stack trace ---",
					"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
				});
			}

			var e3 = Assert.ThrowsException<SerExOk>(proxy.TestSerializedOk);

			Assert.AreEqual("The mess", e3.Message);
			Assert.IsNull(e3.InnerException);

			AssertLines(e3, new string[]
			{
				"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
				"--- End of stack trace from previous location ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
			});
		}

		[TestMethod]
		[DataRow(enSerializer.BinaryFormatter)]
		[DataRow(enSerializer.Json)]
		[DataRow(enSerializer.MemoryPack)]
		[DataRow(enSerializer.MessagePack)]
		[DataRow(enSerializer.Protobuf)]
		public async Task ExceptionTests_InnerEx(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IExceptionTest, ExceptionTest>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IExceptionTest>();

			var e4 = Assert.ThrowsException<SerExOk>(proxy.TestWithInnerException);

			Assert.AreEqual("The mess", e4.Message);
			if (ser == enSerializer.BinaryFormatter)
			{
				Assert.AreEqual("Format of the initialization string does not conform to specification starting at index 0.", e4.InnerException!.Message);
			}
			else
			{
				Assert.IsNull(e4.InnerException);
			}

			AssertLines(e4, new string[]{
					"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
					" ---> System.ArgumentException: Format of the initialization string does not conform to specification starting at index 0.",
					"   --- End of inner exception stack trace ---",
					"--- End of stack trace from previous location ---",
					"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)",
			});
		}

		private void AssertNotLine(Exception e4, string v)
		{
			var lines = e4.ToString().Split(Environment.NewLine);
			Assert.IsFalse(lines.Contains(v));
		}

		private void AssertLines(Exception e, string[] strings)
		{
			var strings_list = strings.ToList();

			var lines = e.ToString().Split(Environment.NewLine);

			foreach (var line in lines)
			{
				if (line == strings_list.First())
				{
					strings_list.RemoveAt(0);
					if (!strings_list.Any())
						break;
					continue;
				}
			}

			Assert.IsTrue(strings_list.Count == 0);
		}

		[TestMethod]
		[DataRow(enSerializer.BinaryFormatter)]
		[DataRow(enSerializer.Json)]
		[DataRow(enSerializer.MemoryPack)]
		[DataRow(enSerializer.MessagePack)]
		[DataRow(enSerializer.Protobuf)]
		public async Task ExceptionTests_SqlException(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9196, serverConfig);
			server.RegisterService<IExceptionTest, ExceptionTest>();
			server.Start();

			await using var client = new NativeClient(9196, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IExceptionTest>();

			var e4 = Assert.ThrowsException<SqlException>(proxy.TestWithSqlException);

			Assert.AreEqual("A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)", e4.Message);

			if (ser == enSerializer.BinaryFormatter)
			{
				Assert.AreEqual("The network path was not found.", e4.InnerException!.Message);
			}
			else
			{
				Assert.IsNull(e4.InnerException);
			}

			AssertLines(e4, new string[]{
				"Microsoft.Data.SqlClient.SqlException (0x80131904): A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)",
				" ---> System.ComponentModel.Win32Exception (53): The network path was not found.",
				"--- End of stack trace from previous location ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException[T](Action action, String message, Object[] parameters)"
			});

			AssertNotLine(e4, "   --- End of inner exception stack trace ---");
		}
	}


	public enum enSerializer
	{
		BinaryFormatter = 1,
		MemoryPack = 2,
		Json = 3,
		MessagePack = 4,
		Protobuf = 5
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
				enSerializer.Protobuf => new ProtobufAdapter(),
				_ => throw new NotImplementedException(),
			};
		}






	}
}
