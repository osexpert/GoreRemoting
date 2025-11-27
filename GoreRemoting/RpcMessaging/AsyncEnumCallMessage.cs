using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging;


public class AsyncEnumCallMessage : IMessage
{
	public AsyncEnumCallMessage()
	{

	}

	public AsyncEnumCallMessage(GoreBinaryReader r)
	{
		Deserialize(r);
	}

	public string ParameterName { get; set; }

	public int Position { get; set; }

	//public MessageType MessageType => MessageType.AsyncEnumCall;

	public int CacheKey => Position;

	public void Deserialize(GoreBinaryReader r)
	{
		ParameterName = r.ReadString();
		Position = r.ReadVarInt();
	}

	public void Deserialize(Stack<object?> st)
	{
	}

	public void Serialize(GoreBinaryWriter w, Stack<object?> st)
	{
		w.Write(ParameterName);
		w.WriteVarInt(Position);
	}
}
