using System;
using GoreRemoting.Tests.ExternalTypes;
using MemoryPack;
using MessagePack;
using ProtoBuf;
using static GoreRemoting.Tests.RpcTests;

namespace GoreRemoting.Tests.Tools;

//    [ReturnAsProxy]
public interface ITestService : IBaseService
{
	event Action ServiceEvent;

	string? TestMethod(string arg);

	void TestMethodWithDelegateArg(Action<string> callback);

	void FireServiceEvent();

	//        [OneWay]
	void OneWayMethod();

	void TestExternalTypeParameter(DataClass data);

	string Echo(string text);

	void MethodWithOutParameter(out int counter);

	string? TestReturnNull();

	int TestReferences1(List<TestObj> l1, List<TestObj> l2);

	byte[] TestSendBytes(byte[] inbytes, out byte[] outBytes);

	(DateTime dt, DateTimeOffset off, Guid g, TimeOnly to, DateOnly don, TestEnum4 enu, TimeSpan ts, DateTimeOffset? nullDto) 
		EchoMiscBasicTypes(DateTime dt, DateTimeOffset off, Guid g, TimeOnly to, DateOnly don, TestEnum4 enu, TimeSpan ts, DateTimeOffset? nullDto);

	(DateTime dt, DateTimeOffset off, Guid g, TestEnum4 enu, TimeSpan ts, DateTimeOffset? nullDto)
		EchoMiscBasicTypesNet48(DateTime dt, DateTimeOffset off, Guid g, TestEnum4 enu, TimeSpan ts, DateTimeOffset? nullDto);

	IEnumerable<string> GetIEnumerableYieldStrings();

	IAsyncEnumerable<string> GetIAsyncEnumerableYieldStrings();

}

public enum TestEnum4
{
	Test1,
	Test2,
	Test3
		
}

[Serializable]
[MemoryPackable(GenerateType.CircularReference)]
[MessagePackObject(keyAsPropertyName: true)]
[ProtoContract]
public partial class TestObj
{
	[MemoryPackOrder(0)]
	[ProtoMember(1)]
	public string? Test { get; set; }
}

public interface IBaseService
{
	string BaseEcho(string s);
	int BaseEchoInt(int s);
}
