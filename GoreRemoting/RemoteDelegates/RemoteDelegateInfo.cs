using System.Collections.Generic;

namespace GoreRemoting.RemoteDelegates
{
	/// <summary>
	/// Describes a remote delegate.
	/// </summary>

	public class RemoteDelegateInfo : IGorializer
	{


		private bool _hasResult;

		/// <summary>
		/// Creates a new instance of the RemoteDelegateInfo class.
		/// </summary>
		/// <param name="hasResult">Has result</param>
		public RemoteDelegateInfo(bool hasResult)
		{
			_hasResult = hasResult;
		}

		public RemoteDelegateInfo(GoreBinaryReader r)
		{
			Deserialize(r);
		}



		/// <summary>
		/// HasResult
		/// </summary>
		public bool HasResult => _hasResult;

		public void Deserialize(GoreBinaryReader r)
		{
			_hasResult = r.ReadBoolean();
		}

		public void Deserialize(Stack<object?> st)
		{

		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(_hasResult);
		}
	}


	public class CancellationTokenPlaceholder
	{
	}

}
