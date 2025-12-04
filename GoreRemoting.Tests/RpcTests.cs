using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GoreRemoting.Compression.Lz4;
using GoreRemoting.Tests.ExternalTypes;
using GoreRemoting.Tests.Tools;
using MemoryPack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace GoreRemoting.Tests;

[TestClass]
public class RpcTests
{
	//private readonly ITestOutputHelper _testOutputHelper;



	//public RpcTests(ITestOutputHelper testOutputHelper)
	//{
	//	_testOutputHelper = testOutputHelper;
	//}

	[TestMethod]
	//		[DataRow(enSerializer.BinaryFormatter)] FIXME: TimeOnly, DateOnly missing
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task TestMiscTypes(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<ITestService>();

		var dt = DateTime.Now;
		var dto = DateTimeOffset.Now;
		var g = Guid.NewGuid();
		var to = TimeOnly.FromDateTime(dt);
		var don = DateOnly.FromDateTime(dt);
		var en = TestEnum4.Test2;
		var span = dt.TimeOfDay;

#if NET6_0_OR_GREATER
		var echo = proxy.EchoMiscBasicTypes(dt, dto, g, to, don, en, span, null);
#else
		var echo = proxy.EchoMiscBasicTypesNet48(dt, dto, g, en, span, null);
#endif
		Assert.AreEqual(dt, echo.dt);
		Assert.AreEqual(dto, echo.off);
		Assert.AreEqual(g, echo.g);
#if NET6_0_OR_GREATER
		Assert.AreEqual(to, echo.to);
		Assert.AreEqual(don, echo.don);
#endif
		Assert.AreEqual(en, echo.enu);
		Assert.AreEqual(span, echo.ts);
		Assert.IsNull(echo.nullDto);
	}


	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task TestSendBytes(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<ITestService>();

		var res = proxy.TestSendBytes(new byte[] { 77, 99, 12 }, out var outBytes);

		Assert.IsTrue(Enumerable.SequenceEqual(new byte[] { 1, 2, 3, 4, 42 }, res));
		Assert.IsTrue(Enumerable.SequenceEqual(new byte[] { 77, 99, 12, 32, 42, 66 }, outBytes));
	
	}



	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task TestReferences(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<ITestService>();

		var l1 = new List<TestObj>();
		var l2 = new List<TestObj>();

		var to = new TestObj() { Test = "42" };
		l1.Add(to);
		l1.Add(to);
		l2.Add(to);
		l2.Add(to);

		var result = proxy.TestReferences1(l1, l2);

		if (ser == Serializer.BinaryFormatter)
			Assert.AreEqual(1, result);
		else if (ser == Serializer.Json)
			Assert.AreEqual(1, result);
#if NET6_0_OR_GREATER
		else if (ser == Serializer.MemoryPack)
			Assert.AreEqual(1, result);
#endif
		else if (ser == Serializer.MessagePack)
			Assert.AreEqual(1, result);
		else if (ser == Serializer.Protobuf)
			Assert.AreEqual(4, result);
		else
			throw new NotImplementedException();
	}



	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task ReturnNullWithLz4(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));
		serverConfig.AddCompressor(new Lz4CompressionProvider());

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		var cliConf = new ClientConfig(Serializers.GetSerializer(ser));
		cliConf.AddCompressor(new Lz4CompressionProvider());
		await using var client = new NativeClient(port, cliConf);

		var proxy = client.CreateProxy<ITestService>();

		var s0 = Stopwatch.StartNew();
		var result = proxy.TestReturnNull();
		s0.Stop();

		var s = Stopwatch.StartNew();
		var result2 = proxy.TestReturnNull();
		s.Stop();

		var s1 = Stopwatch.StartNew();
		var result3 = proxy.Echo("f");
		s1.Stop();

		var s2 = Stopwatch.StartNew();
		var result4 = proxy.Echo("h");
		s2.Stop();

		Assert.IsNull(result);
	}



	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Inherited_methods_should_be_called_correctly(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<ITestService>();

		var result = proxy.BaseEcho("lol");
		var result2 = proxy.BaseEchoInt(3);

		Assert.AreEqual("lol", result);
		Assert.AreEqual(3, result2);
	}


	[TestMethod]
	[DataRow(Serializer.BinaryFormatter, false)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack, false)]
#endif
	[DataRow(Serializer.Json, false)]
	[DataRow(Serializer.MessagePack, false)]
	[DataRow(Serializer.BinaryFormatter, true)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack, true)]
#endif
	[DataRow(Serializer.Json, true)]
	[DataRow(Serializer.MessagePack, true)]
	[DataRow(Serializer.Protobuf, false)]
	[DataRow(Serializer.Protobuf, true)]
	public async Task Call_on_Proxy_should_be_invoked_on_remote_service(Serializer ser, bool compress)
	{
		bool remoteServiceCalled = false;

		var testService =
			new TestService()
			{
				TestMethodFake = arg =>
				{
					remoteServiceCalled = true;
					return arg;
				}
			};

		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser))
			{
				CreateService = (_, _) => new(testService, false)
			};
		if (compress)
			serverConfig.AddCompressor(new Lz4CompressionProvider());

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.Start();
		server.RegisterService<ITestService, TestService>();

		async Task ClientAction()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var cc = new ClientConfig(Serializers.GetSerializer(ser));
			if (compress)
				cc.AddCompressor(new Lz4CompressionProvider()); // default since only 1

			await using var client = new NativeClient(port, cc);

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			//client.Connect();

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var proxy = client.CreateProxy<ITestService>();

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var result = proxy.TestMethod("test");

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var result2 = proxy.TestMethod("test");

			stopWatch.Stop();

			Assert.AreEqual("test", result);
			Assert.AreEqual("test", result2);

			proxy.MethodWithOutParameter(out int methodCallCount);

			Assert.AreEqual(1, methodCallCount);
		}

		var clientThread = new Thread(async () =>
		{
			await ClientAction();
		});
		clientThread.Start();
		clientThread.Join();

		Assert.IsTrue(remoteServiceCalled);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Call_on_Proxy_should_be_invoked_on_remote_service_without_MessageEncryption(Serializer ser)
	{
		bool remoteServiceCalled = false;

		var testService =
			new TestService()
			{
				TestMethodFake = arg =>
				{
					remoteServiceCalled = true;
					return arg;
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

		async Task ClientAction()
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var proxy = client.CreateProxy<ITestService>();

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var result = proxy.TestMethod("test");

			stopWatch.Stop();
			stopWatch.Reset();
			stopWatch.Start();

			var result2 = proxy.TestMethod("test");

			stopWatch.Stop();

			Assert.AreEqual("test", result);
			Assert.AreEqual("test", result2);
		}

		var clientThread = new Thread(async () =>
		{
			await ClientAction();
		});
		clientThread.Start();
		clientThread.Join();

		Assert.IsTrue(remoteServiceCalled);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Delegate_invoked_on_server_should_callback_client(Serializer ser)
	{
		string? argumentFromServer = null;

		var testService = new TestService();

		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		async Task ClientAction()
		{
			await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<ITestService>();
			proxy.TestMethodWithDelegateArg(arg => argumentFromServer = arg);
		}

		var clientThread = new Thread(async () =>
		{
			await ClientAction();
		});
		clientThread.Start();
		clientThread.Join();

		Assert.AreEqual("test", argumentFromServer);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Events_should_NOT_work_remotly(Serializer ser)
	{
		var testService = new TestService();

		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser))
			{
				CreateService = (_, _) => new(testService, false)
			};

		bool serviceEventCalled = false;

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<ITestService>();

		// Does not support this. But maybe we should fail better than we do currently?
		// Calling a delegate in client from server is in GoreRemoting only supported while the call is active,
		// because only then is the callback channel open.
		proxy.ServiceEvent += () => serviceEventCalled = true;

		var ex = Assert.ThrowsExactly<Exception>(proxy.FireServiceEvent);

		Assert.AreEqual("Too late, result sent", ex.Message);

		Assert.IsFalse(serviceEventCalled);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task External_types_should_work_as_remote_service_parameters(Serializer ser)
	{
		bool remoteServiceCalled = false;
		DataClass? parameterValue = null;

		var testService =
			new TestService()
			{
				TestExternalTypeParameterFake = arg =>
				{
					remoteServiceCalled = true;
					parameterValue = arg;
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

		async Task ClientAction()
		{
			await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<ITestService>();
			proxy.TestExternalTypeParameter(new DataClass() { Value = 42 });

			Assert.AreEqual(42, parameterValue!.Value);
		}

		var clientThread = new Thread(async () =>
		{
			await ClientAction();
		});
		clientThread.Start();
		clientThread.Join();

		Assert.IsTrue(remoteServiceCalled);
	}

	#region Service with generic method

	public interface IGenericEchoService
	{
		string Echo(string value);
		List<int> Echo2(List<int> value);
		void a();
		Type EchoType(Type t);
		DeleType EchoTypeWithDelegate(DeleType dt);
	}

	[Serializable]
	public class DeleType
	{
		public Delegate? Test;
	}

	public class GenericEchoService : IGenericEchoService
	{
		public string Echo(string value)
		{
			return value;
		}

		public void a()
		{
			// test smallest payload
		}

		public List<int> Echo2(List<int> value)
		{
			return value;
		}

		public Type EchoType(Type t)
		{
			return t;
		}

		public DeleType EchoTypeWithDelegate(DeleType dt)
		{
			dt.Test = Dt_Test;
			return dt;
		}

		private int Dt_Test()
		{
			throw new NotImplementedException();
		}
	}

	#endregion

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Generic_methods_should_be_called_correctly(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IGenericEchoService, GenericEchoService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IGenericEchoService>();

		var result = proxy.Echo("Yay");
		Assert.AreEqual("Yay", result);

		//var result2 = proxy.Echo(42);
		//Assert.Equal(42, result2);

		var result3 = proxy.Echo2(new List<int> { 1, 2, 3 });
		Assert.HasCount(3, result3);
		Assert.AreEqual(1, result3[0]);
		Assert.AreEqual(2, result3[1]);
		Assert.AreEqual(3, result3[2]);

		proxy.a();

		// only works with binary formatter
		//Assert.Equal(typeof(string), proxy.EchoType(typeof(string)));

		//          var dtt = new DeleType();
		//			dtt.Test = Dtt_Test;
		//          var dtte = proxy.EchoTypeWithDelegate(dtt);
	}

	private int Dtt_Test()
	{
		throw new NotImplementedException();
	}

	#region Service with enum as operation argument

	public enum TestEnum
	{
		First = 1,
		Second = 2
	}

	public interface IEnumTestService
	{
		TestEnum Echo(TestEnum inputValue);
	}

	public class EnumTestService : IEnumTestService
	{
		public TestEnum Echo(TestEnum inputValue)
		{
			return inputValue;
		}
	}

	#endregion

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Enum_arguments_should_be_passed_correctly(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IEnumTestService, EnumTestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IEnumTestService>();

		var resultFirst = proxy.Echo(TestEnum.First);
		var resultSecond = proxy.Echo(TestEnum.Second);

		Assert.AreEqual(TestEnum.First, resultFirst);
		Assert.AreEqual(TestEnum.Second, resultSecond);
	}



	public interface IRefTestService
	{
		void EchoRef(ref string refValue);
		string EchoOut(out string outValue);
	}

	public class RefTestService : IRefTestService
	{
		public void EchoRef(ref string refValue)
		{
			refValue = "bad to the bone";
		}
		public string EchoOut(out string outValue)
		{
			outValue = "I am out";
			return "result";
		}
	}


	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Ref_param_should_fail(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IRefTestService, RefTestService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IRefTestService>();

		string aString = "test";
		Assert.ThrowsExactly<NotSupportedException>(() => proxy.EchoRef(ref aString));
		Assert.AreEqual("test", aString);

		var r = proxy.EchoOut(out var outstr);
		Assert.AreEqual("result", r);
		Assert.AreEqual("I am out", outstr);
	}



	public interface IDelegateTest
	{
		string Test(Func<string, string> echo);
	}


	public class DelegateTest : IDelegateTest
	{
		public string Test(Func<string, string> echo)
		{
			var t = new Thread(() =>
			{
				Test_Thread_Started = true;
				try
				{
					Exception? ex = null;
					while (true)
					{
						try
						{
							var r = echo("hi");
							Assert.AreEqual("hihi", r);

							Test_Thread_DidRun = true;
						}
						catch (Exception e)
						{
							ex = e;


							// various exceptions seen here
							// Exception: "No delegate request data"
							// e can sometimes be null!

							//                        var eWasNull = e == null;
							//                        var masIsNoReqData = e?.Message == "No delegate request data";
							//                        var mess = e?.Message;
							//                        var messIsNull = mess == null;
							//                        var inner = e?.InnerException;
							//                        var b = eWasNull || messIsNull || masIsNoReqData;
							//Assert.True(b);

							Test_Thread_Callback_Failed = true;
							//Assert.True(e is ChannelClosedException);

							break;
						}
					}
					Assert.AreEqual("Too late, result sent", ex.Message);
				}
				finally
				{
					Test_Thread_Done = true;
				}
			});

			t.Start();
			Thread.Sleep(1000);

			while (!Test_Thread_Started)
				Thread.Sleep(10);

			Test_HasReturned = true;
			return "bollocks";
		}
	}

	volatile static bool Test_HasReturned = false;
	volatile static bool Test_Thread_Started = false;
	volatile static bool Test_Thread_DidRun = false;
	volatile static bool Test_Thread_Callback_Failed = false;
	volatile static bool Test_Thread_Done = false;


	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task Delegate_callback_after_return(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IDelegateTest, DelegateTest>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IDelegateTest>();

		bool wasHere = false;

		var res = proxy.Test((e) =>
		{

			wasHere = true;
			return e + "hi";
		});

		Assert.AreEqual("bollocks", res);

		Assert.IsTrue(wasHere);
		Assert.IsTrue(Test_HasReturned);
		Assert.IsTrue(Test_Thread_DidRun);

		while (!Test_Thread_Done)
		{
			Thread.Sleep(10);
		}
		Assert.IsTrue(Test_Thread_Done);
		Assert.IsTrue(Test_Thread_Callback_Failed);
	}

	public interface IDelegateTest2
	{
		string Test(Func<S1, R1> echo, Func<S2, R2> echo2, Func<S3, R3> echo3);
		string Test2(Action<S1> echo, Func<S2, R2> echo2, Action<S3> echo3);
		string Test3(Action<S1> echo, Func<S2, Task<R2>> echo2, Action<S3> echo3);
	}


	public class DelegateTest2 : IDelegateTest2
	{
		volatile bool run = true;

		public string Test(Func<S1, R1> echo, Func<S2, R2> echo2, Func<S3, R3> echo3)
		{
			int i = 0, i1 = 0, i2 = 0;
			var t1 = new Thread(() =>
			{
				while (run)
				{
					var r = echo(new S1("hello"));
					Assert.AreEqual("hellohi", r.r1);
					i++;
				}
			});
			t1.Start();
			var t2 = new Thread(() =>
			{
				while (run)
				{
					var r = echo2(new S2("Yhello"));
					Assert.AreEqual("Yhellohi", r.r2);
					i1++;
				}
			});
			t2.Start();
			var t3 = new Thread(() =>
			{
				while (run)
				{
					var r = echo3(new S3("Xhello"));
					Assert.AreEqual("Xhellohi", r.r3);
					i2++;
				}
			});
			t3.Start();

			Thread.Sleep(1000);

			Assert.IsGreaterThan(0, i);
			Assert.IsGreaterThan(0, i1);
			Assert.IsGreaterThan(0, i2);

			run = false;

			t1.Join();
			t2.Join();
			t3.Join();

			return "רזו";
		}

		public string Test2(Action<S1> echo, Func<S2, R2> echo2, Action<S3> echo3)
		{
			int i = 0, i1 = 0, i2 = 0;
			var t1 = new Thread(() =>
			{
				while (run)
				{
					//var r = 
					echo(new S1("hello"));
					//Assert.Equal("hello", r.r1);
					i++;
				}
			});
			t1.Start();
			var t2 = new Thread(() =>
			{
				while (run)
				{
					var r = echo2(new S2("Yhello"));
					Assert.AreEqual("Yhellohi", r.r2);
					i1++;
				}
			});
			t2.Start();
			var t3 = new Thread(() =>
			{
				while (run)
				{
					//var r = 
					echo3(new S3("Xhello"));
					//Assert.Equal("Xhello", r.r3);
					i2++;
				}
			});
			t3.Start();

			Thread.Sleep(1000);

			Assert.IsGreaterThan(0, i);
			Assert.IsGreaterThan(0, i1);
			Assert.IsGreaterThan(0, i2);

			run = false;

			t1.Join();
			t2.Join();
			t3.Join();

			return "רזו";
		}

		public string Test3(Action<S1> echo, Func<S2, Task<R2>> echo2, Action<S3> echo3)
		{
			Exception? ex = null;

			int i = 0, i1 = 0, i2 = 0;
			var t1 = new Thread(() =>
			{
				while (run)
				{
					//var r = 
					echo(new S1("hello"));
					//Assert.Equal("hello", r.r1);
					i++;
				}
			});
			t1.Start();
			var t2 = new Thread(async () =>
			{
#pragma warning disable MSTEST0040
				try
				{
					while (run)
					{
						var r = await echo2(new S2("Yhello"));
						Assert.AreEqual("Yhellohi", r.r2);
						i1++;
					}
				}
				catch (Exception e)
				{
					ex = e;
				}
#pragma warning restore MSTEST0040
			});
			t2.Start();
			var t3 = new Thread(() =>
			{
				while (run)
				{
					//var r = 
					echo3(new S3("Xhello"));
					//Assert.Equal("Xhello", r.r3);
					i2++;
				}
			});
			t3.Start();

			Thread.Sleep(1000);

			Assert.IsGreaterThan(0, i);
			Assert.IsGreaterThan(0, i1);
			Assert.IsGreaterThan(0, i2);

			run = false;

			t1.Join();
			t2.Join();
			t3.Join();

			if (ex != null)
				Assert.Fail();

			return "רזו";
		}
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task MultipleDelegateCallback(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IDelegateTest2, DelegateTest2>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IDelegateTest2>();

		bool wasHere = false;
		bool wasHere2 = false;
		bool wasHere3 = false;

		Exception? ex1 = null;
		try
		{
			var res1 = proxy.Test((e) =>
			{

				wasHere = true;
				return new(e.s1 + "hi");
			}, (e2) =>
			{

				wasHere2 = true;
				return new(e2.s2 + "hi");
			}, (e3) =>
			{

				wasHere3 = true;
				return new(e3.s3 + "hi");
			});

			Assert.AreEqual("רזו", res1);
		}
		catch (Exception e)
		{
			ex1 = e;
		}

		Assert.IsTrue(wasHere);
		Assert.IsTrue(wasHere2);
		Assert.IsTrue(wasHere3);

//		Assert.AreEqual("Only one delegate with result is supported", ex1!.Message);

		wasHere = false;
		wasHere2 = false;
		wasHere3 = false;

		var res = proxy.Test2((e) =>
		{

			wasHere = true;
			//return new(e + "hi");
		}, (e2) =>
		{

			wasHere2 = true;
			return new(e2.s2 + "hi");
		}, (e3) =>
		{

			wasHere3 = true;
			//return new(e3 + "hi");
		});

		Assert.AreEqual("רזו", res);

		Assert.IsTrue(wasHere);
		Assert.IsTrue(wasHere2);
		Assert.IsTrue(wasHere3);


		wasHere = false;
		wasHere2 = false;
		wasHere3 = false;

		res = proxy.Test3((e) =>
		{

			wasHere = true;
			//return new(e + "hi");
		}, async (e2) =>
		{

			wasHere2 = true;
			return new(e2.s2 + "hi");
		}, (e3) =>
		{

			wasHere3 = true;
			//return new(e3 + "hi");
		});

		Assert.AreEqual("רזו", res);

		Assert.IsTrue(wasHere);
		Assert.IsTrue(wasHere2);
		Assert.IsTrue(wasHere3);


	}

	public interface IVarArgTest
	{
		int[] Test0(bool isProtobuf, int a, params int[] b);

		Task<int> Test1(Func<int, Task<int>> lol);

		ValueTask<int> Test2(Func<int, ValueTask<int>> lol);


		Task<int> Test3(Func<int> f, Action a, Func<Task> a2, Func<ValueTask> vt);
		Task Test4(Action a, Func<Task<int>> a2, Func<ValueTask> vt);
		ValueTask<int> Test5(Func<int> f, Action a, Func<Task> a2, Func<ValueTask<int>> vt);
		ValueTask Test6(Action f, Action a, Func<Task> a2, Func<ValueTask<int>> vt);


		Task Throw1(Action a);
		Task Throw2(Func<Task> a);
		Task Throw3(Func<ValueTask> a);
		Task Throw4(Func<int> a);
		Task Throw5(Func<int, int> a);
		Task Throw6(Func<int, Task> a);
		Task Throw7(Func<int, Task<int>> a);
		Task Throw8(Func<int, ValueTask> a);
		Task Throw9(Func<int, ValueTask<int>> a);
	}

	public class VarArgTest : IVarArgTest
	{
		public int[] Test0(bool isProtobuf, int a, params int[] b)
		{
			var l = new List<int>();
			l.Add(a);

			// ARGH!!!
			if (isProtobuf && b == null)
				b = new int[] { };

			l.AddRange(b);
			return l.ToArray();
		}

		public async Task<int> Test1(Func<int, Task<int>> lol)
		{
			try
			{
				await Task.Delay(1000);
				var res = await lol(42);
				await Task.Delay(1000);
				Assert.AreEqual(422, res);
				return res;
			}
			catch (Exception)
			{
				throw;
			}


		}

		public async ValueTask<int> Test2(Func<int, ValueTask<int>> lol)
		{
			try
			{
				await Task.Delay(1000);
				var res = await lol(42);
				await Task.Delay(1000);
				Assert.AreEqual(422, res);
				return res;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<int> Test3(Func<int> f, Action a, Func<Task> a2, Func<ValueTask> vt)
		{
			var r1 = f();
			Assert.AreEqual(42, r1);
			a();
			await a2();
			await vt();
			return 44;
		}

		public async Task Test4(Action a, Func<Task<int>> a2, Func<ValueTask> vt)
		{
			a();
			var i1 = await a2();
			Assert.AreEqual(42, i1);
			await vt();
		}

		public async ValueTask<int> Test5(Func<int> f, Action a, Func<Task> a2, Func<ValueTask<int>> vt)
		{
			var r1 = f();
			Assert.AreEqual(42, r1);
			a();
			await a2();
			await vt();
			return 44;
		}

		public async ValueTask Test6(Action f, Action a, Func<Task> a2, Func<ValueTask<int>> vt)
		{
			f();
			a();
			await a2();
			var r = await vt();
			Assert.AreEqual(42, r);
		}

		public async Task Throw1(Action a)
		{
			await Task.CompletedTask;
			try
			{
				a();
			}
			catch (Exception e)
			{
				throw1Ex = e;
			}
		}

		public async Task Throw2(Func<Task> a)
		{
			try
			{
				await a();
			}
			catch (Exception e)
			{
				throw2Ex = e;
			}
		}

		public async Task Throw3(Func<ValueTask> a)
		{
			try
			{
				await a();
			}
			catch (Exception e)
			{
				throw3Ex = e;
			}
		}

		public async Task Throw4(Func<int> a)
		{
			await Task.CompletedTask;
			try
			{
				var i = a();
			}
			catch (Exception e)
			{
				throw4Ex = e;
			}
		}

		public async Task Throw5(Func<int, int> a)
		{
			await Task.CompletedTask;
			try
			{
				var i = a(42);
			}
			catch (Exception e)
			{
				throw5Ex = e;
			}
		}

		public async Task Throw6(Func<int, Task> a)
		{
			await Task.CompletedTask;
			try
			{
				await a(42);
			}
			catch (Exception e)
			{
				throw6Ex = e;
			}
		}

		public async Task Throw7(Func<int, Task<int>> a)
		{
			await Task.CompletedTask;
			try
			{
				var i = await a(42);
			}
			catch (Exception e)
			{
				throw7Ex = e;
			}
		}

		public async Task Throw8(Func<int, ValueTask> a)
		{
			await Task.CompletedTask;
			try
			{
				await a(42);
			}
			catch (Exception e)
			{
				throw8Ex = e;
			}
		}

		public async Task Throw9(Func<int, ValueTask<int>> a)
		{
			await Task.CompletedTask;
			try
			{
				var i = await a(42);
			}
			catch (Exception e)
			{
				throw9Ex = e;
			}
		}


	}

	static Exception? throw1Ex;
	static Exception? throw2Ex;
	static Exception? throw3Ex;
	static Exception? throw4Ex;
	static Exception? throw5Ex;
	static Exception? throw6Ex;
	static Exception? throw7Ex;
	static Exception? throw8Ex;
	static Exception? throw9Ex;

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task DoVarArgTest(Serializer ser)
	{
		var port = Ports.GetNext();
		await using var server = new NativeServer(port, new ServerConfig(Serializers.GetSerializer(ser)));
		server.RegisterService<IVarArgTest, VarArgTest>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IVarArgTest>();
		{
			var r1 = proxy.Test0(ser == Serializer.Protobuf, 1);
			Assert.HasCount(1, r1);
			Assert.AreEqual(1, r1[0]);
		}

		{
			var r2 = proxy.Test0(ser == Serializer.Protobuf, 1, 2, 3);
			Assert.HasCount(3, r2);
			Assert.AreEqual(1, r2[0]);
			Assert.AreEqual(2, r2[1]);
			Assert.AreEqual(3, r2[2]);
		}

		{
			var r3 = proxy.Test0(ser == Serializer.Protobuf, 1, 2);
			Assert.HasCount(2, r3);
			Assert.AreEqual(1, r3[0]);
			Assert.AreEqual(2, r3[1]);
		}

		{
			var v = await proxy.Test1(async (a) =>
			{
				Assert.AreEqual(42, a);
				await Task.CompletedTask;
				await Task.Delay(1000);
				return 422;
			});
			Assert.AreEqual(422, v);
		}

		{
			var v2 = await proxy.Test2(async (a) =>
			{
				Assert.AreEqual(42, a);
				await Task.CompletedTask;
				await Task.Delay(1000);
				return 422;
			});
			Assert.AreEqual(422, v2);
		}

		{
			bool t3_called1 = false;
			bool t3_called2 = false;
			bool t3_called3 = false;
			var i3r = await proxy.Test3(() => 42, () =>
			{
				t3_called1 = true;
			},
			async () =>
			{
				await Task.CompletedTask;
				t3_called2 = true;
			},
			async () =>
			{
				await Task.CompletedTask;
				t3_called3 = true;
			});
			Assert.IsTrue(t3_called1);
			Assert.IsTrue(t3_called2);
			Assert.IsTrue(t3_called3);
			Assert.AreEqual(44, i3r);
		}

		{
			bool t4_called1 = false;
			bool t4_called2 = false;
			bool t4_called3 = false;
			await proxy.Test4(() =>
			{
				t4_called1 = true;
			},
			async () =>
			{
				await Task.CompletedTask;
				t4_called2 = true;
				return 42;
			},
			async () =>
			{
				await Task.CompletedTask;
				t4_called3 = true;
			});
			Assert.IsTrue(t4_called1);
			Assert.IsTrue(t4_called2);
			Assert.IsTrue(t4_called3);
		}

		//bool t5_failed = false;
		try
		{
			bool t5_called1 = false;
			bool t5_called2 = false;
			bool t5_called3 = false;
			bool t5_called4 = false;
			var t5ir = await proxy.Test5(() =>
			{
				t5_called1 = true;
				return 42;
			},
			() =>
			{
				t5_called2 = true;
			}
			,
			async () =>
			{
				await Task.CompletedTask;
				t5_called3 = true;

			},
			async () =>
			{
				await Task.CompletedTask;
				t5_called4 = true;
				return 42;
			});
			Assert.IsTrue(t5_called1);
			Assert.IsTrue(t5_called2);
			Assert.IsTrue(t5_called3);
			Assert.IsTrue(t5_called4);
			Assert.AreEqual(44, t5ir);
		}
		catch (Exception e)
		{
			throw;
	//		t5_failed = e.Message == "Only one delegate with result is supported";
		}
//		Assert.IsTrue(t5_failed);

		{
			bool t6_called1 = false;
			bool t6_called2 = false;
			bool t6_called3 = false;
			bool t6_called4 = false;
			await proxy.Test6(() =>
			{
				t6_called1 = true;
			},
			() =>
			{
				t6_called2 = true;
			}
			,
			async () =>
			{
				await Task.CompletedTask;
				t6_called3 = true;

			},
			async () =>
			{
				await Task.CompletedTask;
				t6_called4 = true;
				return 42;
			});
			Assert.IsTrue(t6_called1);
			Assert.IsTrue(t6_called2);
			Assert.IsTrue(t6_called3);
			Assert.IsTrue(t6_called4);
		}


		await proxy.Throw1(() =>
		{
			throw new Exception("test");
		});

		await proxy.Throw2(async () =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		await proxy.Throw3(async () =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		await proxy.Throw4(() =>
		{
			throw new Exception("test");
		});

		await proxy.Throw5(i =>
		{
			throw new Exception("test");
		});

		await proxy.Throw6(async i =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		await proxy.Throw7(async i =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		await proxy.Throw8(async i =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		await proxy.Throw9(async i =>
		{
			await Task.CompletedTask;
			throw new Exception("test");
		});

		Assert.IsNull(throw1Ex);
		Assert.IsNull(throw2Ex);
		Assert.IsNull(throw3Ex);
		Assert.AreEqual("test", throw4Ex!.Message);
		Assert.AreEqual("test", throw5Ex!.Message);
		Assert.IsNull(throw6Ex);
		Assert.AreEqual("test", throw7Ex!.Message);
		Assert.IsNull(throw8Ex);
		Assert.AreEqual("test", throw9Ex!.Message);
	}



	[TestMethod]
	//[DataRow(enSerializer.BinaryFormatter)] does nor work, complain the generated iterator class is not serializable
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task IEnumerableYield(Serializer ser)
	{
		string? argumentFromServer = null;

		var testService = new TestService();

		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<ITestService, TestService>();
		server.Start();

		async Task ClientAction()
		{
			await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<ITestService>();
			argumentFromServer = string.Join(",", proxy.GetIEnumerableYieldStrings().ToList());
		}

		var clientThread = new Thread(async () =>
		{
			await ClientAction();
		});
		clientThread.Start();
		clientThread.Join();

		Assert.AreEqual("a,b", argumentFromServer);
	}


}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class S1
{
	[ProtoMember(1)]
	public string? s1;
	public S1(string s)
	{
		s1 = s;
	}

	[MemoryPackConstructor]
	public S1()
	{
	}
}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class S2
{
	[ProtoMember(1)]
	public string? s2;
	public S2(string s)
	{
		s2 = s;
	}

	[MemoryPackConstructor]
	public S2()
	{
	}
}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class S3
{
	[ProtoMember(1)]
	public string? s3;
	public S3(string s)
	{
		s3 = s;
	}

	[MemoryPackConstructor]
	public S3()
	{
	}
}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class R1
{
	[ProtoMember(1)]
	public string? r1;
	public R1(string r)
	{
		r1 = r;
	}

	[MemoryPackConstructor]
	public R1()
	{
	}
}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class R2
{
	[ProtoMember(1)]
	public string? r2;
	public R2(string r)
	{
		r2 = r;
	}

	[MemoryPackConstructor]
	public R2()
	{
	}
}

[Serializable]
[MemoryPackable]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class R3
{
	[ProtoMember(1)]
	public string? r3;

	[MemoryPackConstructor]
	public R3()
	{
	}

	public R3(string r)
	{
		r3 = r;
	}
}



