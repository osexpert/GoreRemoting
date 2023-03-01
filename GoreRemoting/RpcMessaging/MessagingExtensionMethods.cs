using System;
using System.Collections.Generic;

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
        public static (object[] parameterValues, Type[] parameterTypes) UnwrapParametersFromDeserializedMethodCallMessage(
            this MethodCallMessage callMessage)
        {
            var parameterTypes = new Type[callMessage.Arguments.Length];
            var parameterValues = new object[callMessage.Arguments.Length];

            for (int i = 0; i < callMessage.Arguments.Length; i++)
            {
                var parameter = callMessage.Arguments[i];
                var parameterType = Type.GetType(parameter.TypeName);
                if (parameterType == null)
                    throw new Exception("Parameter type not found: " + parameter.TypeName);
                parameterTypes[i] = parameterType;
                parameterValues[i] = parameter.Value;
            }

            return (parameterValues, parameterTypes);
        }
    }
}