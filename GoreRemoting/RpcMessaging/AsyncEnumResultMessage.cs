using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting.RpcMessaging;


public class AsyncEnumResultMessage : IMessage
{
	/// <summary>
	/// Gets or sets the return value of the invoked method.
	/// </summary>
	public object? Value { get; set; }

	/// <summary>
	/// If Values is a single object, this is 0.
	/// If > 0, then this is a list/array (IEnumerable>)
	/// Added to support sending more than one results at a time (in the future)
	/// </summary>
	public int ListValues { get; set; }

	public MessageType MessageType => MessageType.AsyncEnumResult;

	public int CacheKey => Position;

	public int Position { get; internal set; }
	public bool StreamingDone { get; internal set; }
	public string ParameterName { get; internal set; }

	public AsyncEnumResultMessage()
	{
	}

	public AsyncEnumResultMessage(GoreBinaryReader r)
	{
		Deserialize(r);
	}

	public void Deserialize(GoreBinaryReader r)
	{
		ParameterName = r.ReadString();
		Position = r.ReadVarInt();
		StreamingDone = r.ReadBoolean();

		ListValues = r.ReadVarInt();
	}

	public void Deserialize(Stack<object?> st)
	{
		Value = st.Pop();
	}

	public void Serialize(GoreBinaryWriter w, Stack<object?> st)
	{
		w.Write(ParameterName);
		w.WriteVarInt(Position);
		w.Write(StreamingDone);

		w.WriteVarInt(ListValues);

		st.Push(Value);
	}
}
