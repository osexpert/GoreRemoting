using System;
using System.Runtime.Serialization;

namespace GoreRemoting.RpcMessaging
{
    /// <summary>
    /// Describes a method call as serializable message.
    /// </summary>
    [Serializable]
    public class MethodCallMessage
    {
        /// <summary>
        /// Gets or sets the name of the remote service that should be called.
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the remote method that should be called.
        /// </summary>
        public string MethodName { get; set; }
        
        /// <summary>
        /// Gets or sets an array of messages that describes the parameters that should be passed to the remote method.
        /// </summary>
        //public MethodCallParameterMessage[] Parameters { get; set; }

        public MethodCallArgument[] Arguments { get; set; }

		/// <summary>
		/// Gets or sets an array of call context entries that should be send to the server.
		/// </summary>
		//public CallContextEntry[] CallContextSnapshot { get; set; }

		/// <summary>
		/// Gets or sets an array of generic type parameter names.
		/// </summary>
		//public string[] GenericArgumentTypeNames { get; set; }

        public bool IsGenericMethod { get; set; }
    }
}