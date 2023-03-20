using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace GoreRemoting
{
	[Serializable]
	public class RemoteInvocationException : Exception
	{
		public string TypeName { get; }

		public RemoteInvocationException(string message, string typeName) : base(message)
		{
			TypeName = typeName;
		}

		/// <summary>
		/// Creates a new instance of the RemoteInvocationException class.
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerEx">Optional inner exception</param>
		//public RemoteInvocationException(string message = "Remote invocation failed.", Exception innerEx = null) :
		//	base(message, innerEx)
		//{
		//}

		/// <summary>
		/// Without this constructor, deserialization will fail. 
		/// </summary>
		/// <param name="info">Serialization info</param>
		/// <param name="context">Streaming context</param>
		public RemoteInvocationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	public class ExceptionHelper
	{
		public static Exception ConstructException(string message, Type type)
		{
			Exception newE = null;
			if (type != null)
			{
				// can this fail? missing ctor? yes, can fail...MissingMethodException
				// TODO: be smarter and try to find a ctor with a string, else an empty ctor?

				var ct1 = type.GetConstructor(new Type[] { typeof(string) });
				if (ct1 != null)
					newE = (Exception)ct1.Invoke(new object[] { message! });

				if (newE == null)
				{
					var ct2 = type.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
					if (ct2 != null)
						newE = (Exception)ct2.Invoke(new object[] { message!, null! });
				}

				// I do not want to loose the message
				//if (newE == null)
				//{
				//	var ct3 = type.GetConstructor(new Type[] { });
				//	if (ct3 != null)
				//		newE = (Exception)ct3.Invoke(new object[] { }); // no message??
				//}
			}

			return newE;
		}

		public static FieldInfo GetRemoteStackTraceString()
		{
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			return remoteStackTraceString;
		}

	}
}
