using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging
{
	public class DelegateResultMessage : IGorializer//, IServiceMethod
	{
		public string ParameterName { get; set; }
		public int Position { get; set; }

		// TODO: could have enum with Result or Exception?
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
//			if (ReturnKind != ResultKind.ResultVoid)
			Value = st.Pop();
	//		else
		//		Value = null;
			//Exception = st.Pop();
		}
		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			w.WriteVarInt(Position);

			w.Write((byte)ReturnKind);
			w.Write((byte)StreamingStatus);

//			if (ReturnKind != ResultKind.ResultVoid)
			st.Push(Value);

			//st.Push(Exception);
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
