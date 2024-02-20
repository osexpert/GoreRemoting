using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging
{
	/// <summary>
	/// Serializable message that describes the result of a remote method call.
	/// </summary>
	public class MethodResultMessage : IGorializer
	{
		public MethodResultMessage()
		{
		}

		public MethodResultMessage(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		/// <summary>
		/// Gets or sets the return value of the invoked method.
		/// 
		/// TODO: enum with Result or Exception?
		/// </summary>
		public object? ReturnValue { get; set; }

		/// <summary>
		/// Exception
		/// </summary>
		public object? Exception { get; set; }

		/// <summary>
		/// Gets or sets an array of out parameters.
		/// </summary>
		public MethodOutArgument[] OutArguments { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			var n = r.ReadVarInt();
			OutArguments = new MethodOutArgument[n];
			for (int i = 0; i < n; i++)
				OutArguments[i] = new MethodOutArgument(r);

			var c = r.ReadVarInt();
			CallContextSnapshot = new CallContextEntry[c];
			for (int j = 0; j < c; j++)
				CallContextSnapshot[j] = new CallContextEntry(r);
		}

		public void Deserialize(Stack<object?> st)
		{
			ReturnValue = st.Pop();
			Exception = st.Pop();

			foreach (var oa in OutArguments)
				oa.Deserialize(st);

			foreach (var cc in CallContextSnapshot)
				cc.Deserialize(st);
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			st.Push(ReturnValue);
			st.Push(Exception);

			if (OutArguments == null)
				w.WriteVarInt(0);
			else
			{
				w.WriteVarInt(OutArguments.Length);
				foreach (var oa in OutArguments)
					oa.Serialize(w, st);
			}

			if (CallContextSnapshot == null)
				w.WriteVarInt(0);
			else
			{
				w.WriteVarInt(CallContextSnapshot.Length);
				foreach (var cc in CallContextSnapshot)
					cc.Serialize(w, st);
			}
		}

		/// <summary>
		/// Gets or sets a snapshot of the call context that flows from server back to the client. 
		/// </summary>
		public CallContextEntry[] CallContextSnapshot { get; set; }
	}
}
