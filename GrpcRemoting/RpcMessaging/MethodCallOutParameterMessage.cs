using System;
using System.Runtime.Serialization;

namespace GrpcRemoting.RpcMessaging
{
    /// <summary>
    /// Serializable message that describes an out parameter.
    /// </summary>
    [Serializable]
    public class MethodCallOutArgument
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string ParameterName { get; set; }
        
        /// <summary>
        /// Gets or sets the out value of the parameter.
        /// </summary>
        public object OutValue { get; set; }
    }
}