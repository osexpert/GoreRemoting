using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging
{


	public enum ResultKind
	{
		ResultValue = 1,
		ResultVoid = 2,
		Exception = 3,
	}

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

		public ResultKind ResultType;

		/// <summary>
		/// Gets or sets the return value of the invoked method.
		/// </summary>
		public object? Value { get; set; }

		/// <summary>
		/// Gets or sets an array of out parameters.
		/// </summary>
		public MethodOutArgument[] OutArguments { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			ResultType = (ResultKind)r.ReadByte();

			if (ResultType != ResultKind.Exception)
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
		}

		public void Deserialize(Stack<object?> st)
		{
			if (ResultType == ResultKind.Exception || ResultType == ResultKind.ResultValue)
				Value = st.Pop();
			else
				Value = null;

			if (ResultType != ResultKind.Exception)
			{
				foreach (var oa in OutArguments)
					oa.Deserialize(st);
			}

			foreach (var cc in CallContextSnapshot)
				cc.Deserialize(st);
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write((byte)ResultType);

			if (ResultType == ResultKind.Exception)
			{
				if (Value == null)
					throw new Exception("Exception without value");

				st.Push(Value);
			}
			else if (ResultType == ResultKind.ResultVoid)
			{
				if (Value != null)
					throw new Exception("ResultVoid with value");
			}
			else if (ResultType == ResultKind.ResultValue)
			{
				st.Push(Value);
			}
			else
				throw new Exception("Unknown resultType: " + ResultType);

			if (ResultType != ResultKind.Exception)
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
		}

		/// <summary>
		/// Gets or sets a snapshot of the call context that flows from server back to the client. 
		/// </summary>
		public CallContextEntry[] CallContextSnapshot { get; set; }

	}
}
