namespace GoreRemoting.RpcMessaging
{
	public class DelegateResultMessage : IMessage
	{
		public string ParameterName { get; set; }
		public int Position { get; set; }

		public object? Value { get; set; }

		public DelegateResultType ReturnKind;

		public StreamingStatus StreamingStatus { get; set; }

		public MessageType MessageType => MessageType.DelegateResult;

		public int CacheKey => (int)ReturnKind + (Position * 10);

		public bool IsException => ReturnKind == DelegateResultType.Exception
			|| ReturnKind == DelegateResultType.Exception_dict_internal;

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			w.WriteVarInt(Position);

			var localKind = ReturnKind;

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
				w.Write((byte)StreamingStatus);
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

			ReturnKind = (DelegateResultType)r.ReadByte();

			if (ReturnKind == DelegateResultType.ReturnValue)
			{
				StreamingStatus = (StreamingStatus)r.ReadByte();
			}
			else if (ReturnKind == DelegateResultType.Exception_dict_internal)
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
			if (ReturnKind == DelegateResultType.ReturnValue || ReturnKind == DelegateResultType.Exception)
				Value = st.Pop();
		}

	}

	public enum DelegateResultType
	{
		ReturnValue = 1,
//		ReturnVoid = 2,
		Exception = 3,
		Exception_dict_internal = 4,
	}

	public enum StreamingStatus
	{
		None = 0,
		Active = 1,
		Done = 2
	}
}
