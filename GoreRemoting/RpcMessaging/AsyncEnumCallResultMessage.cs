using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting.RpcMessaging;


public class AsyncEnumCallResultMessage : IMessage
{
	public string ParameterName { get; set; }
	public int Position { get; set; }

	public object? Value { get; set; }

	public DelegateResultType ResultType { get; set; } = DelegateResultType.ReturnValue;

	public bool StreamingDone { get; set; }

	//public MessageType MessageType => MessageType.AsyncEnumCallResult;

	public int CacheKey => (int)ResultType + (Position * 10);

	public bool IsException => ResultType == DelegateResultType.Exception
		|| ResultType == DelegateResultType.Exception_dict_internal;

	public void Serialize(GoreBinaryWriter w, Stack<object?> st)
	{
		w.Write(ParameterName);
		w.WriteVarInt(Position);

		var localKind = ResultType;

		IDictionary<string, string>? dict = null;

		if (localKind == DelegateResultType.Exception)
		{
			if (Value is IDictionary<string, string> d)
			{
				localKind = DelegateResultType.Exception_dict_internal;
				dict = d;
			}
			else
			{
				st.Push(Value);
			}
		}
		else if (localKind == DelegateResultType.ReturnValue)
		{
			st.Push(Value);
		}

		w.Write((byte)localKind);

		if (localKind == DelegateResultType.ReturnValue)
			w.Write(StreamingDone);
		else if (localKind == DelegateResultType.Exception_dict_internal)
		{
			w.WriteVarInt(dict.Count);
			foreach (var kv in dict)
			{
				w.Write(kv.Key);
				w.Write(kv.Value);
			}
		}
	}

	public void Deserialize(GoreBinaryReader r)
	{
		ParameterName = r.ReadString();
		Position = r.ReadVarInt();

		ResultType = (DelegateResultType)r.ReadByte();

		if (ResultType == DelegateResultType.ReturnValue)
		{
			StreamingDone = r.ReadBoolean();
		}
		else if (ResultType == DelegateResultType.Exception_dict_internal)
		{
			var n = r.ReadVarInt();
			Dictionary<string, string> dict = new(n);
			for (int i = 0; i < n; i++)
			{
				var k = r.ReadString();
				var v = r.ReadString();
				dict.Add(k, v);
			}

			Value = dict;
		}
	}
	public void Deserialize(Stack<object?> st)
	{
		if (ResultType == DelegateResultType.ReturnValue || ResultType == DelegateResultType.Exception)
			Value = st.Pop();
	}
}

