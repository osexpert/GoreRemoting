using GoreRemoting.RemoteDelegates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace GoreRemoting.RpcMessaging
{
	/// <summary>
	/// Serializable message that describes a parameter of an remote method call. 
	/// </summary>
	public class MethodCallArgument : IGorializer
	{
		public MethodCallArgument()
		{ }

		public MethodCallArgument(GoreBinaryReader r)
		{
			Deserialize(r);
		}

		/// <summary>
		/// Gets or sets the name of the parameter.
		/// </summary>
		public string ParameterName { get; set; }

		/// <summary>
		/// Gets or sets the type name of the parameter.
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Gets or sets the parameter value.
		/// </summary>
		public object Value { get; set; }

		bool _popValue;

		public void Deserialize(GoreBinaryReader r)
		{
			ParameterName = r.ReadString();
			TypeName = r.ReadString();

			var v = r.ReadByte();
			_popValue = (v == 0);
			if (!_popValue)
			{
				if (v == 1)
				{
					Value = new RemoteDelegates.RemoteDelegateInfo(r);
				}
				else if (v == 2)
				{
					Value = new CancellationTokenPlaceholder();
				}
				else
					throw new NotImplementedException("unk type");
			}
		}

		public void Deserialize(Stack<object> st)
		{
			if (_popValue)
				Value = st.Pop();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write(ParameterName);
			w.Write(TypeName);

			if (Value is IGorializer g)
			{
				if (Value is RemoteDelegates.RemoteDelegateInfo)
				{
					w.Write((byte)1);
				}
				else
					throw new NotImplementedException("unk type");

				g.Serialize(w, st);
			}
			else if (Value is CancellationTokenPlaceholder)
			{
				w.Write((byte)2);
			}
			else
			{
				w.Write((byte)0);
				st.Push(Value);
			}
		}
	}
}
