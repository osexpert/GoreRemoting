namespace GoreRemoting.RpcMessaging
{

	/// <summary>
	/// Describes a method call as serializable message.
	/// </summary>
	public class MethodCallMessage : IMessage
	{

		public MethodCallArgument[] Arguments { get; set; }

		/// <summary>
		/// Gets or sets an array of call context entries that should be send to the server.
		/// </summary>
		public CallContextEntry[] CallContextSnapshot { get; set; }

		public MessageType MessageType => MessageType.MethodCall;

		public int CacheKey => 0;

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.WriteVarInt(Arguments.Length);
			foreach (var a in Arguments)
				a.Serialize(w, st);

			if (CallContextSnapshot == null)
				w.WriteVarInt(0);
			else
			{
				w.WriteVarInt(CallContextSnapshot.Length);
				foreach (var entry in CallContextSnapshot)
					entry.Serialize(w, st);
			}
		}

		public void Deserialize(GoreBinaryReader r)
		{
			var n = r.ReadVarInt();
			Arguments = new MethodCallArgument[n];
			for (int i = 0; i < n; i++)
				Arguments[i] = new MethodCallArgument(r);

			var c = r.ReadVarInt();
			CallContextSnapshot = new CallContextEntry[c];
			for (int j = 0; j < c; j++)
				CallContextSnapshot[j] = new CallContextEntry(r);
		}

		public void Deserialize(Stack<object?> st)
		{
			foreach (var a in Arguments)
				a.Deserialize(st);

			foreach (var cc in CallContextSnapshot)
				cc.Deserialize(st);
		}

		public object?[] ParameterValues()
		{
			var parameterValues = new object?[this.Arguments.Length];

			for (int i = 0; i < this.Arguments.Length; i++)
			{
				var parameter = this.Arguments[i];
				parameterValues[i] = parameter.Value;
			}

			return parameterValues;
		}
	}
}
