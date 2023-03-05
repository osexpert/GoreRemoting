using GoreRemoting.RpcMessaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace GoreRemoting.RemoteDelegates
{
    /// <summary>
    /// Describes a remote delegate.
    /// </summary>

    public class RemoteDelegateInfo : IGorializer
    {
        private string _delegateTypeName;

        private bool _hasResult;

        /// <summary>
        /// Creates a new instance of the RemoteDelegateInfo class.
        /// </summary>
        /// <param name="delegateTypeName">Type name of the client delegate</param>
        /// <param name="hasResult">Has result</param>
		public RemoteDelegateInfo(string delegateTypeName, bool hasResult)
        {
            _delegateTypeName = delegateTypeName;
			_hasResult = hasResult;
        }

		public RemoteDelegateInfo(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		/// <summary>
		/// Gets the type name of the client delegate.
		/// </summary>
		public string DelegateTypeName => _delegateTypeName;

        /// <summary>
        /// HasResult
        /// </summary>
        public bool HasResult => _hasResult;

		public void Deserialize(GoreBinaryReader r)
		{
			_delegateTypeName = r.ReadString();
			_hasResult = r.ReadBoolean();
		}

		public void Deserialize(Stack<object> st)
		{

		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write(_delegateTypeName);
			w.Write(_hasResult);
		}
	}


	public class CancellationTokenDummy : IGorializer
	{
        public CancellationTokenDummy()
        {
            
        }
        public CancellationTokenDummy(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		public void Deserialize(GoreBinaryReader r)
		{
		}

		public void Deserialize(Stack<object> st)
		{
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
		}
	}

}