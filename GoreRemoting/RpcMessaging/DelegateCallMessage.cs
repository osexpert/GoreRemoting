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
			Position = r.ReadVarInt();
			OneWay = r.ReadBoolean();

			var n = r.ReadVarInt();
			Arguments = new object[n];
		}

		public void Deserialize(Stack<object> st)
		{
			for (int i = 0; i < Arguments.Length; i++)
				Arguments[i] = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.WriteVarInt(Position);
			w.Write(OneWay);

			w.WriteVarInt(Arguments.Length);

			foreach (var arg in Arguments)
				st.Push(arg);
		}
	}




}
