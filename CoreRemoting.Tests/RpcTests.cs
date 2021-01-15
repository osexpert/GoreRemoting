using System;
using System.Security;
using System.Threading;
using CoreRemoting.Authentication;
using CoreRemoting.ClassicRemotingApi;
using CoreRemoting.DependencyInjection;
using CoreRemoting.Tests.Tools;
using NUnit.Framework;

namespace CoreRemoting.Tests
{
    public class RpcTests
    {
        [SetUp]
        public void Init()
        {
            RemotingConfiguration.DisableClassicRemotingApi();
        }
        
        [Test]
        [NonParallelizable]
        public void Call_on_Proxy_should_be_invoked_on_remote_service()
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
                    NetworkPort = 9094,
                    RegisterServicesAction = container =>
                        container.RegisterService<ITestService>(
                            factoryDelegate: () => testService,
                            lifetime: ServiceLifetime.Singleton)
                };

            int serverErrorCount = 0;
            
            using var server = new RemotingServer(serverConfig);
            server.Error += (sender, exception) => serverErrorCount++;
            server.Start();

            void ClientAction()
            {
                using var client = new RemotingClient(new ClientConfig() {ConnectionTimeout = 1, ServerPort = 9094});

                client.Connect();

                var proxy = client.CreateProxy<ITestService>();
                var result = proxy.TestMethod("test");

                Assert.AreEqual("test", result);
            }

            var clientThread = new Thread(ClientAction);
            clientThread.Start();
            clientThread.Join();
            
            Assert.IsTrue(remoteServiceCalled);
            Assert.AreEqual(0, serverErrorCount);
        }

        [Test]
        [NonParallelizable]
        public void Delegate_invoked_on_server_should_callback_client()
        {
            string argumentFromServer = null;

            var testService = new TestService();
            
            var serverConfig =
                new ServerConfig()
                {
                    NetworkPort = 9095,
                    RegisterServicesAction = container =>
                        container.RegisterService<ITestService>(
                            factoryDelegate: () => testService,
                            lifetime: ServiceLifetime.Singleton)
                };

            int serverErrorCount = 0;
            
            using var server = new RemotingServer(serverConfig);
            server.Error += (sender, exception) => serverErrorCount++;
            server.Start();

            void ClientAction()
            {
                using var client = new RemotingClient(
                    new ClientConfig()
                    {
                        ConnectionTimeout = 1, 
                        ServerPort = 9095
                    });

                client.Connect();

                var proxy = client.CreateProxy<ITestService>();
                proxy.TestMethodWithDelegateArg(arg => argumentFromServer = arg);
            }

            var clientThread = new Thread(ClientAction);
            clientThread.Start();
            clientThread.Join();
                
            Assert.AreEqual("test", argumentFromServer);
            Assert.AreEqual(0, serverErrorCount);
        }
        
        [Test]
        [NonParallelizable]
        public void Events_should_work_remotly()
        {
            var testService = new TestService();
            
            var serverConfig =
                new ServerConfig()
                {
                    NetworkPort = 9096,
                    RegisterServicesAction = container =>
                        container.RegisterService<ITestService>(
                            factoryDelegate: () => testService,
                            lifetime: ServiceLifetime.Singleton)
                };

            int serverErrorCount = 0;
            bool serviceEventCalled = false;
            
            using var server = new RemotingServer(serverConfig);
            server.Error += (sender, exception) => serverErrorCount++;
            server.Start();
            
            using var client = new RemotingClient(
                new ClientConfig()
                {
                    ConnectionTimeout = 1, 
                    ServerPort = 9096
                });

            client.Connect();

            var proxy = client.CreateProxy<ITestService>();
            
            proxy.ServiceEvent += () => 
                serviceEventCalled = true;
            
            proxy.FireServiceEvent();

            Assert.IsTrue(serviceEventCalled);
            Assert.AreEqual(0, serverErrorCount);
        }
    }
}