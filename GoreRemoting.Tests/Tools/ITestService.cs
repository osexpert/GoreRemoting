using System;
using GoreRemoting.Tests.ExternalTypes;
using MemoryPack;
using MessagePack;

namespace GoreRemoting.Tests.Tools
{
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
	}

	[Serializable]
	[MemoryPackable(GenerateType.CircularReference)]
	[MessagePackObject(true)]
	public partial class TestObj
	{
		[MemoryPackOrder(0)]
		public string Test { get; set; }
	}

	public interface IBaseService
	{
		string BaseEcho(string s);
		int BaseEchoInt(int s);
	}
}
