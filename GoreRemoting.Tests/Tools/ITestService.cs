using System;
using GoreRemoting.Tests.ExternalTypes;

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
	}

	public interface IBaseService
	{
		string BaseEcho(string s);
		int BaseEchoInt(int s);
	}
}