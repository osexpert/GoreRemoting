using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace GoreRemoting.RpcMessaging
{
    /// <summary>
    /// Serializable message that describes the result of a remote method call.
    /// </summary>
    public class MethodResultMessage : IGorializer
    {
        public MethodResultMessage()
        {
        }

        public MethodResultMessage(BinaryReader r)
        {
            Deserialize(r);
        }

        /// <summary>
        /// Gets or sets the return value of the invoked method.
        /// 
        /// TODO: enum with Result or Exception?
        /// </summary>
        public object ReturnValue { get; set; }
        
        /// <summary>
        /// Exception
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets an array of out parameters.
        /// </summary>
        public MethodOutArgument[] OutArguments { get; set; }

		public void Deserialize(BinaryReader r)
		{
            var n = r.ReadInt32();
			OutArguments = new MethodOutArgument[n];
            for (int i = 0; i < n; i++)
				OutArguments[i] = new MethodOutArgument(r);
		}

        public void Deserialize(Stack<object> st)
        {
            ReturnValue = st.Pop();
            Exception = (Exception)st.Pop();

            foreach (var oa in OutArguments)
                oa.Deserialize(st);
        }

		public void Serialize(BinaryWriter w, Stack<object> st)
		{
			st.Push(ReturnValue);
			st.Push(Exception);

            if (OutArguments == null)
                w.Write(0);
            else
            {
                w.Write(OutArguments.Length);
                foreach (var oa in OutArguments)
                    oa.Serialize(w, st);
            }
		}

		/// <summary>
		/// Gets or sets a snapshot of the call context that flows from server back to the client. 
		/// </summary>
		//public CallContextEntry[] CallContextSnapshot { get; set; }
	}
}