#if false
using System;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Tests.Tools;
using Xunit;

namespace GoreRemoting.Tests
{
    public class BinarySerializationTests
    {
        [Fact]
        public void BinarySerializerAdapter_should_deserialize_MethodCallMessage()
        {
            var serializer = new BinarySerializerAdapter();
            var testServiceInterfaceType = typeof(ITestService);
            
            var messageBuilder = new MethodCallMessageBuilder();

            var message =
                messageBuilder.BuildMethodCallMessage(
                    serializer,
                    testServiceInterfaceType.Name,
                    testServiceInterfaceType.GetMethod("TestMethod"),
                    new object[] { 4711});

            var rawData = serializer.Serialize(message);
            
            var deserializedMessage = serializer.Deserialize<MethodCallMessage>(rawData);

            (var parameterValues, var parameterTypes) = deserializedMessage.UnwrapParametersFromDeserializedMethodCallMessage();

            var parametersLength = deserializedMessage.Arguments.Length;
            
            Assert.Equal(1, parametersLength);
            Assert.NotNull(deserializedMessage.Arguments[0]);
            Assert.Equal("arg", deserializedMessage.Arguments[0].ParameterName);
            Assert.StartsWith("System.Object,", deserializedMessage.Arguments[0].TypeName);
            Assert.Equal(typeof(int), parameterValues[0].GetType());
            Assert.Equal(typeof(object), parameterTypes[0]);
            Assert.Equal(4711, parameterValues[0]);
        }

    }
}
#endif