using System.Collections.Generic;

namespace GoreRemoting.RpcMessaging
{
	/// <summary>
	/// Serializable message that describes an out parameter.
	/// </summary>
	public class MethodOutArgument : IGorializer
	{
		public MethodOutArgument()
		{
		}

		public MethodOutArgument(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		/// <summary>
		/// Gets or sets the name of the parameter.
		/// </summary>
		public string ParameterName { get; set; }

		/// <summary>
		/// Gets or sets the out value of the parameter.
		/// </summary>
		public object? OutValue { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			ParameterName = r.ReadString();
		}

		public void Deserialize(Stack<object?> st)
		{
			OutValue = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			st.Push(OutValue);
		}
	}
}