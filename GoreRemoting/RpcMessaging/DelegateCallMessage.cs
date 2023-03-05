using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoreRemoting.RpcMessaging
{

	public class DelegateCallMessage : IGorializer
	{
        public DelegateCallMessage()
        {
            
        }

        public DelegateCallMessage(BinaryReader r)
		{
			Deserialize(r);
		}

		public int Position { get; set; }

		public object[] Arguments { get; set; }

		public bool OneWay { get; set; }

		public void Deserialize(BinaryReader r)
		{
			Position = r.ReadInt32();
			OneWay = r.ReadBoolean();

			var n = r.ReadInt32();
			Arguments = new object[n];
		}

		public void Deserialize(Stack<object> st)
		{
			for (int i = 0; i < Arguments.Length; i++)
				Arguments[i] = st.Pop();
		}

		public void Serialize(BinaryWriter w, Stack<object> st)
		{
			w.Write(Position);
			w.Write(OneWay);

			w.Write(Arguments.Length);

			foreach (var arg in Arguments)
				st.Push(arg);
		}
	}



	public class DelegateResultMessage : IGorializer
	{
		public int Position { get; set; }

		// TODO: could have enum with Result or Exception?
		public object Result { get; set; }

		public Exception Exception { get; set; }

		public void Deserialize(BinaryReader r)
		{
			Position = r.ReadInt32();
		}
		public void Deserialize(Stack<object> st)
		{
			Result = st.Pop();
			Exception = (Exception)st.Pop();
		}
		public void Serialize(BinaryWriter w, Stack<object> st)
		{
			w.Write(Position);

			st.Push(Result);
			st.Push(Exception);
		}
	}
}
