using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using GoreRemoting.Tests.ExternalTypes;
using GoreRemoting.Tests.Tools;
using Xunit;
using Xunit.Abstractions;
using static GoreRemoting.Tests.RpcTests;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Serialization;
using GoreRemoting.Serialization.MemoryPack;
using MemoryPack;
using GoreRemoting.Serialization.Json;
using GoreRemoting.Serialization.MessagePack;
using MessagePack;
using GoreRemoting.Compression.Lz4;

namespace GoreRemoting.Tests
{
	public class RpcTests
	{
		private readonly ITestOutputHelper _testOutputHelper;



		public RpcTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}









		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Inherited_methods_should_be_called_correctly(enSerializer ser)
		{
			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9095, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			await using var client = new NativeClient(9095, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<ITestService>();

			var result = proxy.BaseEcho("lol");
			var result2 = proxy.BaseEchoGen<int>(3);

			Assert.Equal("lol", result);
			Assert.Equal(3, result2);
		}






		[Theory]
		[InlineData(enSerializer.BinaryFormatter, false)]
		[InlineData(enSerializer.MemoryPack, false)]
		[InlineData(enSerializer.Json, false)]
		[InlineData(enSerializer.MessagePack, false)]
		[InlineData(enSerializer.BinaryFormatter, true)]
		[InlineData(enSerializer.MemoryPack, true)]
		[InlineData(enSerializer.Json, true)]
		[InlineData(enSerializer.MessagePack, true)]
		public async Task Call_on_Proxy_should_be_invoked_on_remote_service(enSerializer ser, bool compress)
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
					GetService = (a) => testService
				};
			if (compress)
				serverConfig.AddCompressor(new Lz4CompressionProvider());

			await using var server = new NativeServer(9094, serverConfig);
			server.Start();
			server.RegisterService<ITestService, TestService>();

			async Task ClientAction()
			{
				try
				{
					var stopWatch = new Stopwatch();
					stopWatch.Start();

					var cc = new ClientConfig(Serializers.GetSerializer(ser));
					if (compress)
						cc.AddCompressor(new Lz4CompressionProvider()); // default since only 1

					await using var client = new NativeClient(9094, cc);

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Creating client took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					//client.Connect();

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Establishing connection took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var proxy = client.CreateProxy<ITestService>();

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Creating proxy took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var result = proxy.TestMethod("test");

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Remote method invocation took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var result2 = proxy.TestMethod("test");

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Second remote method invocation took {stopWatch.ElapsedMilliseconds} ms");

					Assert.Equal("test", result);
					Assert.Equal("test", result2);

					proxy.MethodWithOutParameter(out int methodCallCount);

					Assert.Equal(1, methodCallCount);
				}
				catch (Exception e)
				{
					_testOutputHelper.WriteLine(e.ToString());
					throw;
				}
			}

			var clientThread = new Thread(async () =>
			{
				await ClientAction();
			});
			clientThread.Start();
			clientThread.Join();

			Assert.True(remoteServiceCalled);
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Call_on_Proxy_should_be_invoked_on_remote_service_without_MessageEncryption(enSerializer ser)
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
					GetService = (a) => testService
				};


			await using var server = new NativeServer(9094, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			async Task ClientAction()
			{
				try
				{
					var stopWatch = new Stopwatch();
					stopWatch.Start();

					await using var client = new NativeClient(9094, new ClientConfig(Serializers.GetSerializer(ser)));

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Creating client took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					//client.Connect();

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Establishing connection took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var proxy = client.CreateProxy<ITestService>();

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Creating proxy took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var result = proxy.TestMethod("test");

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Remote method invocation took {stopWatch.ElapsedMilliseconds} ms");
					stopWatch.Reset();
					stopWatch.Start();

					var result2 = proxy.TestMethod("test");

					stopWatch.Stop();
					_testOutputHelper.WriteLine($"Second remote method invocation took {stopWatch.ElapsedMilliseconds} ms");

					Assert.Equal("test", result);
					Assert.Equal("test", result2);
				}
				catch (Exception e)
				{
					_testOutputHelper.WriteLine(e.ToString());
					throw;
				}
			}

			var clientThread = new Thread(async () =>
			{
				await ClientAction();
			});
			clientThread.Start();
			clientThread.Join();

			Assert.True(remoteServiceCalled);
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Delegate_invoked_on_server_should_callback_client(enSerializer ser)
		{
			string argumentFromServer = null;

			var testService = new TestService();

			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9095, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			async Task ClientAction()
			{
				try
				{
					await using var client = new NativeClient(9095, new ClientConfig(Serializers.GetSerializer(ser)));

					var proxy = client.CreateProxy<ITestService>();
					proxy.TestMethodWithDelegateArg(arg => argumentFromServer = arg);
				}
				catch (Exception e)
				{
					_testOutputHelper.WriteLine(e.ToString());
					throw;
				}
			}

			var clientThread = new Thread(async () =>
			{
				await ClientAction();
			});
			clientThread.Start();
			clientThread.Join();

			Assert.Equal("test", argumentFromServer);
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Events_should_NOT_work_remotly(enSerializer ser)
		{
			var testService = new TestService();

			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser))
				{
					GetService = (a) => testService
				};

			bool serviceEventCalled = false;

			await using var server = new NativeServer(9096, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			await using var client = new NativeClient(9096, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<ITestService>();

			// Does not support this. But maybe we should fail better than we do currently?
			// Calling a delegate in client from server is in GoreRemoting only supported while the call is active,
			// because only then is the callback channel open.
			proxy.ServiceEvent += () => serviceEventCalled = true;

			//Assert.Throws<System.Threading.Channels.ChannelClosedException>(() => proxy.FireServiceEvent());

			Exception ex = null;
			try
			{
				proxy.FireServiceEvent();
			}
			catch (Exception e)
			{
				ex = e;
			}
			Assert.Equal("Too late, result sent", ex.Message);
			Assert.Equal(15, ex.ToString().Split(Environment.NewLine).Length);

			Assert.False(serviceEventCalled);
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task External_types_should_work_as_remote_service_parameters(enSerializer ser)
		{
			bool remoteServiceCalled = false;
			DataClass parameterValue = null;

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
					GetService = (a) => testService
				};


			await using var server = new NativeServer(9097, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

			async Task ClientAction()
			{
				try
				{
					await using var client = new NativeClient(9097, new ClientConfig(Serializers.GetSerializer(ser)));

					var proxy = client.CreateProxy<ITestService>();
					proxy.TestExternalTypeParameter(new DataClass() { Value = 42 });

					Assert.Equal(42, parameterValue.Value);
				}
				catch (Exception e)
				{
					_testOutputHelper.WriteLine(e.ToString());
					throw;
				}
			}

			var clientThread = new Thread(async () =>
			{
				await ClientAction();
			});
			clientThread.Start();
			clientThread.Join();

			Assert.True(remoteServiceCalled);
		}

		#region Service with generic method

		public interface IGenericEchoService
		{
			T Echo<T>(T value);

			List<T> Echo2<T>(List<T> value);

			void a();

			Type EchoType(Type t);

			DeleType EchoTypeWithDelegate(DeleType dt);
		}

		[Serializable]
		public class DeleType
		{
			public Delegate Test;
		}

		public class GenericEchoService : IGenericEchoService
		{
			public T Echo<T>(T value)
			{
				return value;
			}

			public void a()
			{
				// test smallest payload
			}

			public List<T> Echo2<T>(List<T> value)
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

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Generic_methods_should_be_called_correctly(enSerializer ser)
		{
			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9197, serverConfig);
			server.RegisterService<IGenericEchoService, GenericEchoService>();
			server.Start();

			await using var client = new NativeClient(9197, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IGenericEchoService>();

			var result = proxy.Echo("Yay");
			Assert.Equal("Yay", result);

			var result2 = proxy.Echo(42);
			Assert.Equal(42, result2);

			var result3 = proxy.Echo2(new List<int> { 1, 2, 3 });
			Assert.Equal(3, result3.Count);
			Assert.Equal(1, result3[0]);
			Assert.Equal(2, result3[1]);
			Assert.Equal(3, result3[2]);

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

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Enum_arguments_should_be_passed_correctly(enSerializer ser)
		{
			var serverConfig =
				new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IEnumTestService, EnumTestService>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IEnumTestService>();

			var resultFirst = proxy.Echo(TestEnum.First);
			var resultSecond = proxy.Echo(TestEnum.Second);

			Assert.Equal(TestEnum.First, resultFirst);
			Assert.Equal(TestEnum.Second, resultSecond);
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


		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Ref_param_should_fail(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IRefTestService, RefTestService>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IRefTestService>();

			string aString = "test";
			Assert.Throws<NotSupportedException>(() => proxy.EchoRef(ref aString));
			Assert.Equal("test", aString);

			var r = proxy.EchoOut(out var outstr);
			Assert.Equal("result", r);
			Assert.Equal("I am out", outstr);
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
						Exception ex = null;


						while (true)
						{

							try
							{

								var r = echo("hi");
								Assert.Equal("hihi", r);

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

						Assert.Equal("Too late, result sent", ex.Message);


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


		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task Delegate_callback_after_return(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IDelegateTest, DelegateTest>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IDelegateTest>();

			bool wasHere = false;

			var res = proxy.Test((e) =>
			{

				wasHere = true;
				return e + "hi";
			});

			Assert.Equal("bollocks", res);

			Assert.True(wasHere);
			Assert.True(Test_HasReturned);
			Assert.True(Test_Thread_DidRun);

			while (!Test_Thread_Done)
			{
				Thread.Sleep(10);
			}
			Assert.True(Test_Thread_Done);
			Assert.True(Test_Thread_Callback_Failed);
		}




		public interface IDelegateTest2
		{
			string Test(Func<S1, R1> echo, Func<S2, R2> echo2, Func<S3, R3> echo3);
			string Test(Action<S1> echo, Func<S2, R2> echo2, Action<S3> echo3);
		}


		public class DelegateTest2 : IDelegateTest2
		{

			volatile bool run = true;

			public string Test(Func<S1, R1> echo, Func<S2, R2> echo2, Func<S3, R3> echo3)
			{
				// can't get here
				Assert.True(false);

				throw new NotImplementedException();
			}

			public string Test(Action<S1> echo, Func<S2, R2> echo2, Action<S3> echo3)
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
						Assert.Equal("Yhellohi", r.r2);
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

				Assert.True(i > 0);
				Assert.True(i1 > 0);
				Assert.True(i2 > 0);


				run = false;

				t1.Join();
				t2.Join();
				t3.Join();

				return "רזו";
			}
		}

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task MultipleDelegateCallback(enSerializer ser)
		{
			var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IDelegateTest2, DelegateTest2>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IDelegateTest2>();

			bool wasHere = false;
			bool wasHere2 = false;
			bool wasHere3 = false;

			Exception ex1 = null;
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

				Assert.Equal("רזו", res1);
			}
			catch (Exception e)
			{
				ex1 = e;
			}

			Assert.False(wasHere);
			Assert.False(wasHere2);
			Assert.False(wasHere3);

			Assert.True(ex1.Message == "Only one delegate with result is supported");

			wasHere = false;
			wasHere2 = false;
			wasHere3 = false;

			var res = proxy.Test((e) =>
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

			Assert.Equal("רזו", res);

			Assert.True(wasHere);
			Assert.True(wasHere2);
			Assert.True(wasHere3);
		}

		public interface IVarArgTest
		{
			int[] Test(int a, params int[] b);

			Task<int> Test(Func<int, Task<int>> lol);

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
			public int[] Test(int a, params int[] b)
			{
				var l = new List<int>();
				l.Add(a);
				l.AddRange(b);
				return l.ToArray();
			}

			public async Task<int> Test(Func<int, Task<int>> lol)
			{
				try
				{
					await Task.Delay(1000);
					var res = await lol(42);
					await Task.Delay(1000);
					Assert.Equal(422, res);
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
					Assert.Equal(422, res);
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
				Assert.Equal(42, r1);
				a();
				await a2();
				await vt();
				return 44;
			}

			public async Task Test4(Action a, Func<Task<int>> a2, Func<ValueTask> vt)
			{
				a();
				var i1 = await a2();
				Assert.Equal(42, i1);
				await vt();
			}

			public async ValueTask<int> Test5(Func<int> f, Action a, Func<Task> a2, Func<ValueTask<int>> vt)
			{
				var r1 = f();
				Assert.Equal(42, r1);
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
				Assert.Equal(42, r);
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

		static Exception throw1Ex;
		static Exception throw2Ex;
		static Exception throw3Ex;
		static Exception throw4Ex;
		static Exception throw5Ex;
		static Exception throw6Ex;
		static Exception throw7Ex;
		static Exception throw8Ex;
		static Exception throw9Ex;

		[Theory]
		[InlineData(enSerializer.BinaryFormatter)]
		[InlineData(enSerializer.MemoryPack)]
		[InlineData(enSerializer.Json)]
		[InlineData(enSerializer.MessagePack)]
		public async Task DoVarArgTest(enSerializer ser)
		{
			await using var server = new NativeServer(9198, new ServerConfig(Serializers.GetSerializer(ser)));
			server.RegisterService<IVarArgTest, VarArgTest>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

			var proxy = client.CreateProxy<IVarArgTest>();
			{
				var r1 = proxy.Test(1);
				Assert.Single(r1);
				Assert.Equal(1, r1[0]);
			}

			{
				var r2 = proxy.Test(1, 2, 3);
				Assert.Equal(3, r2.Length);
				Assert.Equal(1, r2[0]);
				Assert.Equal(2, r2[1]);
				Assert.Equal(3, r2[2]);
			}

			{
				var r3 = proxy.Test(1, 2);
				Assert.Equal(2, r3.Length);
				Assert.Equal(1, r3[0]);
				Assert.Equal(2, r3[1]);
			}

			{
				var v = await proxy.Test(async (a) =>
				{
					Assert.Equal(42, a);
					await Task.CompletedTask;
					await Task.Delay(1000);
					return 422;
				});
				Assert.Equal(422, v);
			}

			{
				var v2 = await proxy.Test2(async (a) =>
				{
					Assert.Equal(42, a);
					await Task.CompletedTask;
					await Task.Delay(1000);
					return 422;
				});
				Assert.Equal(422, v2);
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
				Assert.True(t3_called1);
				Assert.True(t3_called2);
				Assert.True(t3_called3);
				Assert.Equal(44, i3r);
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
				Assert.True(t4_called1);
				Assert.True(t4_called2);
				Assert.True(t4_called3);
			}

			bool t5_failed = false;
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
				Assert.True(t5_called1);
				Assert.True(t5_called2);
				Assert.True(t5_called3);
				Assert.True(t5_called4);
				Assert.Equal(44, t5ir);
			}
			catch (Exception e)
			{
				t5_failed = e.Message == "Only one delegate with result is supported";
			}
			Assert.True(t5_failed);

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
				Assert.True(t6_called1);
				Assert.True(t6_called2);
				Assert.True(t6_called3);
				Assert.True(t6_called4);
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

			Assert.Null(throw1Ex);
			Assert.Null(throw2Ex);
			Assert.Null(throw3Ex);
			Assert.Equal("test", throw4Ex.Message);
			Assert.Equal("test", throw5Ex.Message);
			Assert.Null(throw6Ex);
			Assert.Equal("test", throw7Ex.Message);
			Assert.Null(throw8Ex);
			Assert.Equal("test", throw9Ex.Message);
		}
	}

	[Serializable]
	[MemoryPackable]
	[MessagePackObject(keyAsPropertyName: true)]
	public partial class S1
	{
		public string s1;
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
	public partial class S2
	{
		public string s2;
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
	public partial class S3
	{
		public string s3;
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
	public partial class R1
	{
		public string r1;
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
	public partial class R2
	{
		public string r2;
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
	public partial class R3
	{
		public string r3;

		[MemoryPackConstructor]
		public R3()
		{

		}

		public R3(string r)
		{
			r3 = r;
		}

	}




}
