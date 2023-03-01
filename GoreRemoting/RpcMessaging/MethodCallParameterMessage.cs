using System;
using System.Runtime.Serialization;

namespace GoreRemoting.RpcMessaging
{
    /// <summary>
    /// Serializable message that describes a parameter of an remote method call. 
    /// </summary>
    [Serializable]
    public class MethodCallArgument
    {
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
    }
}