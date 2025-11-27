using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging;


public class AsyncEnumReturnResultMessage : IMessage
{
	public object? Value { get; set; }

	public AsyncEnumReturnResultMessage()
	{

	}

	public AsyncEnumReturnResultMessage(GoreBinaryReader r)
	{
		Deserialize(r);
	}

	//public MessageType MessageType => MessageType.AsyncEnumReturnResult;

	public int CacheKey => 0;

	public void Deserialize(GoreBinaryReader r)
	{
	}

	public void Deserialize(Stack<object?> st)
	{
		Value = st.Pop();
	}

	public void Serialize(GoreBinaryWriter w, Stack<object?> st)
	{
		st.Push(Value);
	}
}
