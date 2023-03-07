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

        public MethodResultMessage(GoreBinaryReader r)
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
        public object Exception { get; set; }

        /// <summary>
        /// Gets or sets an array of out parameters.
        /// </summary>
        public MethodOutArgument[] OutArguments { get; set; }

		public void Deserialize(GoreBinaryReader r)
		{
            var n = r.Read7BitEncodedInt();
			OutArguments = new MethodOutArgument[n];
            for (int i = 0; i < n; i++)
				OutArguments[i] = new MethodOutArgument(r);
		}

        public void Deserialize(Stack<object> st)
        {
            ReturnValue = st.Pop();
            Exception = st.Pop();

            foreach (var oa in OutArguments)
                oa.Deserialize(st);
        }

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			st.Push(ReturnValue);
			st.Push(Exception);

            if (OutArguments == null)
                w.Write7BitEncodedInt(0);
            else
            {
                w.Write7BitEncodedInt(OutArguments.Length);
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