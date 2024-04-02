namespace GoreRemoting.RpcMessaging
{
	public enum ResultKind
	{
		ResultValue = 1,
		ResultVoid = 2,
		Exception = 3,
		Exception_dict_internal = 4,
	}

	/// <summary>
	/// Serializable message that describes the result of a remote method call.
	/// </summary>
	public class MethodResultMessage : IMessage
	{
		public MethodResultMessage()
		{
		}

		public MethodResultMessage(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		public ResultKind ResultType;

		/// <summary>
		/// Gets or sets the return value of the invoked method.
		/// </summary>
		public object? Value { get; set; }

		/// <summary>
		/// Gets or sets an array of out parameters.
		/// </summary>
		public MethodOutArgument[] OutArguments { get; set; }

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			var localType = ResultType;

			IDictionary<string, string>? dict = null;

			if (localType == ResultKind.Exception)
			{
				if (Value is IDictionary<string, string> d)
				{
					localType = ResultKind.Exception_dict_internal;
					dict = d;
				}
				else
				{
					st.Push(Value);
				}
			}
			else if (localType == ResultKind.ResultValue)
			{
				st.Push(Value);
			}

			w.Write((byte)localType);

			if (localType == ResultKind.ResultValue || localType == ResultKind.ResultVoid)
			{
				if (OutArguments == null)
					w.WriteVarInt(0);
				else
				{
					w.WriteVarInt(OutArguments.Length);
					foreach (var oa in OutArguments)
						oa.Serialize(w, st);
				}
			}

			if (CallContextSnapshot == null)
				w.WriteVarInt(0);
			else
			{
				w.WriteVarInt(CallContextSnapshot.Length);
				foreach (var cc in CallContextSnapshot)
					cc.Serialize(w, st);
			}

			if (localType == ResultKind.Exception_dict_internal)
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
			ResultType = (ResultKind)r.ReadByte();

			if (ResultType == ResultKind.ResultValue || ResultType == ResultKind.ResultVoid)
			{
				var n = r.ReadVarInt();
				OutArguments = new MethodOutArgument[n];
				for (int i = 0; i < n; i++)
					OutArguments[i] = new MethodOutArgument(r);
			}

			var c = r.ReadVarInt();
			CallContextSnapshot = new CallContextEntry[c];
			for (int j = 0; j < c; j++)
				CallContextSnapshot[j] = new CallContextEntry(r);

			if (ResultType == ResultKind.Exception_dict_internal)
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
			if (ResultType == ResultKind.Exception || ResultType == ResultKind.ResultValue)
				Value = st.Pop();

			if (ResultType == ResultKind.ResultValue || ResultType == ResultKind.ResultVoid)
			{
				foreach (var oa in OutArguments)
					oa.Deserialize(st);
			}

			foreach (var cc in CallContextSnapshot)
				cc.Deserialize(st);
		}

		

		/// <summary>
		/// Gets or sets a snapshot of the call context that flows from server back to the client. 
		/// </summary>
		public CallContextEntry[] CallContextSnapshot { get; set; }

		public MessageType MessageType => MessageType.MethodResult;

		public int CacheKey => (int)ResultType;

		public bool IsException => ResultType == ResultKind.Exception
			|| ResultType == ResultKind.Exception_dict_internal;
	}
}
