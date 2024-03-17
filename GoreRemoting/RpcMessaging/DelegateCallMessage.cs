using System.Collections.Generic;

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

		public string ParameterName { get; set; }

		public int Position { get; set; }

		public object?[] Arguments { get; set; }

		public bool OneWay { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			ParameterName = r.ReadString();
			Position = r.ReadVarInt();
			OneWay = r.ReadBoolean();

			var n = r.ReadVarInt();
			Arguments = new object[n];
		}

		public void Deserialize(Stack<object?> st)
		{
			for (int i = 0; i < Arguments.Length; i++)
				Arguments[i] = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			w.WriteVarInt(Position);
			w.Write(OneWay);

			w.WriteVarInt(Arguments.Length);

			foreach (var arg in Arguments)
				st.Push(arg);
		}
	}




}
