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
		/// <summary>
		/// Non qualified Type name (never contains assembly name). Uses Type.ToString()
		/// Same as "ClassName" in the SerializationInfo
		/// </summary>
		public string ClassName { get; internal set; }

		public RemoteInvocationException(string message, string className) : base(message)
		{
			ClassName = className;
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
			// "ClassName" is already taken
			ClassName = info.GetString("GoreClassName");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("GoreClassName", ClassName);
		}
	}


	public class ExceptionHelper
	{
#if false
		public static Exception ConstructException(string message, Type type)
		{
			Exception newE = null;
			if (type != null)
			{
				// can this fail? missing ctor? yes, can fail...MissingMethodException
				// TODO: be smarter and try to find a ctor with a string, else an empty ctor?

				var ct1 = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
				if (ct1 != null)
					newE = (Exception)ct1.Invoke(new object[] { message! });

				if (newE == null)
				{
					var ct2 = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Exception) }, null);
					if (ct2 != null)
						newE = (Exception)ct2.Invoke(new object[] { message!, null! });
				}

				// I do not want to loose the message
				if (newE == null)
				{
					var ct3 = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
					if (ct3 != null)
					{
						newE = (Exception)ct3.Invoke(new object[] { }); // no message

						var msgField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
						msgField.SetValue(newE, message);
					}
				}

				if (newE == null)
				{
					newE = (Exception)FormatterServices.GetUninitializedObject(type);

					var msgField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
					msgField.SetValue(newE, message);
				}
			}

			return newE;
		}
#endif
		public static FieldInfo GetRemoteStackTraceString()
		{
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			return remoteStackTraceString;
		}

	}
}
