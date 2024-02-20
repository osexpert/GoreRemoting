using System;
using System.Collections.Generic;

namespace GoreRemoting
{
	/// <summary>
	/// Describes a single call context entry.
	/// </summary>

	public class CallContextEntry : IGorializer
	{
		public CallContextEntry()
		{
		}

		public CallContextEntry(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		/// <summary>
		/// Gets or sets the name of the call context entry. 
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value of the call context entry.
		/// </summary>
		public object? Value { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			Name = r.ReadString();
		}

		public void Deserialize(Stack<object?> st)
		{
			Value = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(Name);

			st.Push(Value);
		}
	}
}
