using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace GoreRemoting.RpcMessaging
{
    

	internal interface IGorializer
	{
        void Serialize(BinaryWriter w, Stack<object> st);
        void Deserialize(BinaryReader r);
		void Deserialize(Stack<object> st);
	}

    /// <summary>
    /// Describes a method call as serializable message.
    /// </summary>
    public class MethodCallMessage : IGorializer
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

        public void Serialize(BinaryWriter w, Stack<object> st)
        {
            w.Write(ServiceName);
            w.Write(MethodName);
			w.Write(IsGenericMethod);

			w.Write(Arguments.Length);
            foreach (var a in Arguments)
                a.Serialize(w, st);
        }

        public void Deserialize(BinaryReader r)
        {
            ServiceName = r.ReadString();
            MethodName = r.ReadString();
			IsGenericMethod = r.ReadBoolean();

			var n = r.ReadInt32();
            Arguments = new MethodCallArgument[n];
            for (int i = 0; i < n; i++)
                Arguments[i] = new MethodCallArgument(r);
        }

        public void Deserialize(Stack<object> st)
        {
            foreach (var a in Arguments)
                a.Deserialize(st);
        }
    }
}