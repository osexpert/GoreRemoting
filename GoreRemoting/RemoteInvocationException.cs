using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace GoreRemoting
{
	public class RemoteInvocationException : Exception
	{
		public const string PropertyDataKey = "GoreRemoting.PropertyData";

		/// <summary>
		/// Non qualified Type name (never contains assembly name). Uses Type.ToString()
		/// Same as "ClassName" in the SerializationInfo
		/// </summary>
		public string ClassName { get; }

		public IReadOnlyDictionary<string, string> PropertyData { get; }

		public RemoteInvocationException(ExceptionData ed) : base(ed.Message)
		{
			ClassName = ed.ClassName;
			PropertyData = ed.PropertyData;
		}

	
	}



}
