namespace GoreRemoting.RpcMessaging
{
	/// <summary>
	/// Extension methods for messaging.
	/// </summary>
	public static class MessagingExtensionMethods
	{

		/// <summary>
		/// Unwraps parameter values and parameter types from a deserialized MethodCallMessage.
		/// </summary>
		/// <param name="callMessage">MethodCallMessage object</param>
		public static object?[] UnwrapParametersFromDeserializedMethodCallMessage(this MethodCallMessage callMessage)
		{
			var parameterValues = new object?[callMessage.Arguments.Length];

			for (int i = 0; i < callMessage.Arguments.Length; i++)
			{
				var parameter = callMessage.Arguments[i];
				parameterValues[i] = parameter.Value;
			}

			return parameterValues;
		}
	}
}