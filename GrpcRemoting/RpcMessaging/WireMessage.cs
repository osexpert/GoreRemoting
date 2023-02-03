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
	}

	[Serializable]
	public class WireResponseMessage
	{
		public WireResponseMessage(DelegateCallMessage callMsg)
		{
			Delegate = callMsg;
			ResponseType = ResponseType.Delegate;
		}

		public WireResponseMessage(MethodCallResultMessage resultMessage)
		{
			Result = resultMessage;
			ResponseType = ResponseType.Result;
		}

		/// <summary>
		/// Gets or sets the type of the message.
		/// </summary>
		public ResponseType ResponseType { get; set; }

		public MethodCallResultMessage Result { get; set; }

		public DelegateCallMessage Delegate { get; set; }
	}
}
