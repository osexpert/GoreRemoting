using System;
using System.Runtime.Serialization;

namespace GrpcRemoting.RpcMessaging
{
    /// <summary>
    /// Serializable message that describes the result of a remote method call.
    /// </summary>
    [Serializable]
    public class MethodCallResultMessage
    {
        /// <summary>
        /// Gets or sets the return value of the invoked method.
        /// </summary>
        public object ReturnValue { get; set; }
        
        /// <summary>
        /// Exception
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets an array of out parameters.
        /// </summary>
        public MethodCallOutParameterMessage[] OutParameters { get; set; } 
        
        /// <summary>
        /// Gets or sets a snapshot of the call context that flows from server back to the client. 
        /// </summary>
        //public CallContextEntry[] CallContextSnapshot { get; set; }
    }
}