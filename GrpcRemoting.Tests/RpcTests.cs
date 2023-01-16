using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcRemoting.Tests.ExternalTypes;
using GrpcRemoting.Tests.Tools;
using Xunit;
using Xunit.Abstractions;

namespace GrpcRemoting.Tests
{
    public class RpcTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public RpcTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Call_on_Proxy_should_be_invoked_on_remote_service()
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
                new ServerConfig()
                {
                    CreateInstance = (t, c) => testService
                };

            await using var server = new NativeServer(9094, serverConfig);
			server.Start();
			server.RegisterService<ITestService, TestService>();
			
            async Task ClientAction()
            {
                try
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await using var client = new NativeClient(9094, new ClientConfig());

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
        
        [Fact]
        public async Task Call_on_Proxy_should_be_invoked_on_remote_service_without_MessageEncryption()
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
                new ServerConfig()
                {
                    CreateInstance = (t,c) => testService
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

                    await using var client = new NativeClient(9094, new ClientConfig());

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

        [Fact]
        public async Task Delegate_invoked_on_server_should_callback_client()
        {
            string argumentFromServer = null;

            var testService = new TestService();
            
            var serverConfig =
                new ServerConfig()
                {
                };

            await using var server = new NativeServer(9095, serverConfig);
			server.RegisterService<ITestService, TestService>();
			server.Start();

            async Task ClientAction()
            {
                try
                {
                    await using var client = new NativeClient(9095, new ClientConfig());

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
        
        [Fact]
        public async Task Events_should_NOT_work_remotly()
        {
            var testService = new TestService();

            var serverConfig =
                new ServerConfig()
                {
                    CreateInstance = (t, c) => testService
                };

            bool serviceEventCalled = false;
            
            await using var server = new NativeServer(9096, serverConfig);
            server.RegisterService<ITestService, TestService>();
            server.Start();

            await using var client = new NativeClient(9096, new ClientConfig());

            var proxy = client.CreateProxy<ITestService>();
            
            // Does not support this. But maybe we should fail better than we do currently?
            // Calling a delegate in client from server is in GrpcRemoting only supported while the call is active,
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

            Assert.False(serviceEventCalled);
        }
        
        [Fact]
        public async Task External_types_should_work_as_remote_service_parameters()
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
                new ServerConfig()
                {
                    CreateInstance = (t,c) => testService
                };

           
            await using var server = new NativeServer(9097, serverConfig);
            server.RegisterService<ITestService, TestService>();
            server.Start();

            async Task ClientAction()
            {
                try
                {
                    await using var client = new NativeClient(9097, new ClientConfig());

                    var proxy = client.CreateProxy<ITestService>();
                    proxy.TestExternalTypeParameter(new DataClass() {Value = 42});

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

            void a();
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
        }

        #endregion
        
        [Fact]
        public async Task Generic_methods_should_be_called_correctly()
        {
            var serverConfig =
                new ServerConfig()
                {
                };

            await using var server = new NativeServer(9197, serverConfig);
            server.RegisterService<IGenericEchoService, GenericEchoService>();
            server.Start();

            await using var client = new NativeClient(9197, new ClientConfig());

            var proxy = client.CreateProxy<IGenericEchoService>();

            var result = proxy.Echo("Yay");
            
            Assert.Equal("Yay", result);

            proxy.a();
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
        
        [Fact]
        public async Task Enum_arguments_should_be_passed_correctly()
        {
            var serverConfig =
                new ServerConfig()
                {
                };

            await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IEnumTestService, EnumTestService>();
			server.Start();

            await using var client = new NativeClient(9198, new ClientConfig());

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


		[Fact]
		public async Task Ref_param_should_fail()
		{
			var serverConfig =
				new ServerConfig()
				{
				};

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IRefTestService, RefTestService>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig());

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

				Thread.Sleep(100);

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

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern void OutputDebugString(string message);

		[Fact]
        public async Task Delegate_callback_after_return()
        {
			var serverConfig = new ServerConfig()
		    {
		    };

			await using var server = new NativeServer(9198, serverConfig);
			server.RegisterService<IDelegateTest, DelegateTest>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig());

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
	}
}
