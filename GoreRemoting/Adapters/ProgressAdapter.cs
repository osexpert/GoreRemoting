using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting
{
	public static class ProgressAdapter
	{
		public static Action<T> ClientConsume<T>(IProgress<T> p)
		{
			return x => p.Report(x);
		}

		public static IProgress<T> ServerProduce<T>(Action<T> report)
		{
			return new IProgressWrapper<T>(report);
		}

		class IProgressWrapper<T> : IProgress<T>
		{
			Action<T> _report;

			public IProgressWrapper(Action<T> report)
			{
				_report = report;
			}

			public void Report(T value)
			{
				_report(value);
			}
		}
	}

}
