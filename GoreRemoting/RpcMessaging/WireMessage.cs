using System;
using System.Collections.Generic;
using System.IO;

namespace GoreRemoting.RpcMessaging
{

	public enum ResponseType
	{
		/// <summary>
		/// Result
		/// </summary>
		Result,
		/// <summary>
		/// Delegate
		/// </summary>
		Delegate,
	}


	public class WireResponseMessage : IGorializer
	{
		public WireResponseMessage()
		{ }

		public WireResponseMessage(DelegateCallMessage callMsg)
		{
			Delegate = callMsg;
			ResponseType = ResponseType.Delegate;
		}

		public WireResponseMessage(MethodResultMessage resultMessage)
		{
			Result = resultMessage;
			ResponseType = ResponseType.Result;
		}

		/// <summary>
		/// Gets or sets the type of the message.
		/// </summary>
		public ResponseType ResponseType { get; set; }

		public MethodResultMessage Result { get; set; }

		public DelegateCallMessage Delegate { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
			ResponseType = (ResponseType)r.Read7BitEncodedInt();

			if (ResponseType == ResponseType.Delegate)
				Delegate = new DelegateCallMessage(r);
			else if (ResponseType == ResponseType.Result)
				Result = new MethodResultMessage(r);
			else
				throw new NotImplementedException();
		}

		public void Deserialize(Stack<object> st)
		{
			if (ResponseType == ResponseType.Delegate)
				Delegate.Deserialize(st);
			else if (ResponseType == ResponseType.Result)
				Result.Deserialize(st);
			else
				throw new NotImplementedException();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write7BitEncodedInt((int)ResponseType);

			if (ResponseType == ResponseType.Delegate)
				Delegate.Serialize(w, st);
			else if (ResponseType == ResponseType.Result)
				Result.Serialize(w, st);
			else
				throw new NotImplementedException();
		}
	}

}
