using System;

namespace GrpcRemoting.RpcMessaging
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
		/// <summary>
		/// Iterator (IEnumerable yield)
		/// </summary>
		Iterator,
	}

	[Serializable]
	public class WireResponseMessage
	{
		public WireResponseMessage(DelegateCallbackMessage callMsg)
		{
			Delegate = callMsg;
			ResponseType = ResponseType.Delegate;
		}

		public WireResponseMessage(MethodResultMessage resultMessage)
		{
			Result = resultMessage;
			ResponseType = ResponseType.Result;
		}

		public WireResponseMessage(IteratorCallbackMessage iterMessage)
		{
			Iterator = iterMessage;
			ResponseType = ResponseType.Iterator;
		}

		/// <summary>
		/// Gets or sets the type of the message.
		/// </summary>
		public ResponseType ResponseType { get; set; }

		public MethodResultMessage Result { get; set; }

		public DelegateCallbackMessage Delegate { get; set; }

		public IteratorCallbackMessage Iterator { get; set; }
	}

	[Serializable]
	public class IteratorCallbackMessage
	{
		public object Data { get; set; }

		// TODO: should this have final result info too? and exception info? Or use a MethodCallResultMessage for that?
	}

	[Serializable]
	public class IteratorCallbackAckMessage
	{
		//public object Data { get; set; }

		// TODO: should this have final result info too? and exception info? Or use a MethodCallResultMessage for that?

		public Exception Exception { get; set; }
	}
}
