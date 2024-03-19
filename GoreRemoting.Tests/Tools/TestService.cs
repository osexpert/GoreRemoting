using System;
using GoreRemoting.Tests.ExternalTypes;

namespace GoreRemoting.Tests.Tools
{
	public class TestService : ITestService
	{
		private int _counter = 0;

		public Func<string, string>? TestMethodFake { get; set; }

		public Action? OneWayMethodFake { get; set; }

		public Action<DataClass>? TestExternalTypeParameterFake { get; set; }

		public event Action? ServiceEvent;

		public string? TestMethod(string arg)
		{
			return TestMethodFake?.Invoke(arg);
		}

		public void TestMethodWithDelegateArg(Action<string> callback)
		{
			callback("test");
		}

		public void FireServiceEvent()
		{
			ServiceEvent?.Invoke();
		}

		public void OneWayMethod()
		{
			OneWayMethodFake?.Invoke();
		}

		public void TestExternalTypeParameter(DataClass data)
		{
			TestExternalTypeParameterFake?.Invoke(data);
		}

		public string Echo(string text)
		{
			return text;
		}

		public void MethodWithOutParameter(out int counter)
		{
			_counter++;
			counter = _counter;
		}

		public string BaseEcho(string s)
		{
			return s;
		}

		public int BaseEchoInt(int s)
		{
			return s;
		}

		public string? TestReturnNull()
		{
			return null;
		}

		public int TestReferences1(List<TestObj> l1, List<TestObj> l2)
		{
			var g1 = l1.Union(l2).GroupBy(a => a).Count();
			return g1;
		}
	}
}