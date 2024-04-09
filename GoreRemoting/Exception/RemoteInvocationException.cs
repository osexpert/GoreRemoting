using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoreRemoting
{
	public class RemoteInvocationException : Exception
	{
		/// <summary>
		/// Non qualified Type name (never contains assembly name). Uses Type.ToString()
		/// Same as "ClassName" in the SerializationInfo
		/// </summary>
		public string ClassName { get; }

		internal RemoteInvocationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			ClassName = info.GetString(ExceptionConverter.ClassNameKey);
		}
	}



}
