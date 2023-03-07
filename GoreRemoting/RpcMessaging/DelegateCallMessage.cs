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

        public DelegateCallMessage(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		public int Position { get; set; }

		public object[] Arguments { get; set; }

		public bool OneWay { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			Position = r.Read7BitEncodedInt();
			OneWay = r.ReadBoolean();

			var n = r.Read7BitEncodedInt();
			Arguments = new object[n];
		}

		public void Deserialize(Stack<object> st)
		{
			for (int i = 0; i < Arguments.Length; i++)
				Arguments[i] = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write7BitEncodedInt(Position);
			w.Write(OneWay);

			w.Write7BitEncodedInt(Arguments.Length);

			foreach (var arg in Arguments)
				st.Push(arg);
		}
	}



	public class DelegateResultMessage : IGorializer
	{
		public int Position { get; set; }

		// TODO: could have enum with Result or Exception?
		public object Result { get; set; }

		public object Exception { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			Position = r.Read7BitEncodedInt();
		}
		public void Deserialize(Stack<object> st)
		{
			Result = st.Pop();
			Exception = st.Pop();
		}
		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write7BitEncodedInt(Position);

			st.Push(Result);
			st.Push(Exception);
		}
	}
}
