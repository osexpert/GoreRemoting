using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting.RpcMessaging
{
	public class DelegateResultMessage : IGorializer
	{
		public int Position { get; set; }

		// TODO: could have enum with Result or Exception?
		public object Result { get; set; }

		public StreamingStatus StreamingStatus { get; set; }

		public object Exception { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			Position = r.Read7BitEncodedInt();
			StreamingStatus = (StreamingStatus)r.ReadByte();
		}
		public void Deserialize(Stack<object> st)
		{
			Result = st.Pop();
			Exception = st.Pop();
		}
		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write7BitEncodedInt(Position);
			w.Write((byte)StreamingStatus);

			st.Push(Result);
			st.Push(Exception);
		}
	}

	public enum StreamingStatus
	{
		None,
		Active,
		Done
	}
}
