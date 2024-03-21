namespace GoreRemoting.RpcMessaging
{
	public class DelegateResultMessage : IGorializer
	{
		public string ParameterName { get; set; }
		public int Position { get; set; }

		public object? Value { get; set; }

		public DelegateResultType ReturnKind;

		public StreamingStatus StreamingStatus { get; set; }


		public void Deserialize(GoreBinaryReader r)
		{
			ParameterName = r.ReadString();
			Position = r.ReadVarInt();

			ReturnKind = (DelegateResultType)r.ReadByte();
			StreamingStatus = (StreamingStatus)r.ReadByte();
		}
		public void Deserialize(Stack<object?> st)
		{
			Value = st.Pop();
		}
		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			w.WriteVarInt(Position);

			w.Write((byte)ReturnKind);
			w.Write((byte)StreamingStatus);

			st.Push(Value);
		}
	}

	public enum DelegateResultType
	{
		ReturnValue = 1,
//		ReturnVoid = 2,
		Exception = 3
	}

	public enum StreamingStatus
	{
		None = 0,
		Active = 1,
		Done = 2
	}
}
