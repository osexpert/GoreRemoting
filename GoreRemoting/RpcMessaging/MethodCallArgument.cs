using GoreRemoting.RemoteDelegates;

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

		public int Position;

		/// <summary>
		/// Gets or sets the parameter value.
		/// </summary>
		public object? Value { get; set; }

		public bool IsOut;

		bool _popValue;

		public void Deserialize(GoreBinaryReader r)
		{
			ParameterName = r.ReadString();
			Position = r.ReadVarInt();

			ParameterValueType pt = (ParameterValueType)r.ReadByte();

			IsOut = (pt == ParameterValueType.Out);

			if (pt == ParameterValueType.Normal)
			{
				_popValue = true;
			}
			else if (pt == ParameterValueType.RemoteDelegateInfo)
			{
				Value = new RemoteDelegates.RemoteDelegateInfo(r);
			}
			else if (pt == ParameterValueType.CancellationTokenPlaceholder)
			{
				Value = new CancellationTokenPlaceholder();
			}
			else if (pt == ParameterValueType.Out)
			{
				// ignore
				Value = null;
			}
			else
				throw new NotImplementedException("unk type: " + pt);

		}

		public void Deserialize(Stack<object?> st)
		{
			if (_popValue)
			{
				Value = st.Pop();
			}
		}

		public void Serialize(GoreBinaryWriter w, Stack<object?> st)
		{
			w.Write(ParameterName);
			w.WriteVarInt(Position);
			

			if (Value is IGorializer g)
			{
				if (Value is RemoteDelegates.RemoteDelegateInfo)
				{
					w.Write((byte)ParameterValueType.RemoteDelegateInfo);
				}
				else
					throw new NotImplementedException("unk type");

				g.Serialize(w, st);
			}
			else if (Value is CancellationTokenPlaceholder)
			{
				w.Write((byte)ParameterValueType.CancellationTokenPlaceholder);
			}
			else if (IsOut)
			{
				w.Write((byte)ParameterValueType.Out);
			}
			else
			{
				w.Write((byte)ParameterValueType.Normal);
				st.Push(Value);
			}
		}

		enum ParameterValueType
		{
			Normal = 1,
			Out = 2,
			RemoteDelegateInfo = 3,
			CancellationTokenPlaceholder = 4
		}
	}
}
