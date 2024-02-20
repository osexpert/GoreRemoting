using System;


namespace Nerdbank.Streams
{
	internal interface IDisposableObservable
	{
		public bool IsDisposed { get; }
	}

	internal static class Verify
	{
		internal static void NotDisposed(IDisposableObservable d)
		{
			if (d.IsDisposed)
				throw new ObjectDisposedException(d.GetType().FullName);
		}
	}

	internal static class Requires
	{
		internal static void NotNull(object o, string paramName)
		{
			if (o == null)
				throw new ArgumentNullException(paramName);
		}
	}

}
