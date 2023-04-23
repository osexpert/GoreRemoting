﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace GoreRemoting
{
	[Serializable]
	public class RemoteInvocationException : Exception
	{
		public const string GoreRemotingPropertyDataKey = "GoreRemoting.PropertyData";
		public const string GoreRemotingClassNameKey = "GoreRemoting.ClassName";

		/// <summary>
		/// Non qualified Type name (never contains assembly name). Uses Type.ToString()
		/// Same as "ClassName" in the SerializationInfo
		/// </summary>
		public string ClassName { get; }

		public IReadOnlyDictionary<string, string> PropertyData { get; }

		public RemoteInvocationException(string message, string className, Dictionary<string, string> propData) : base(message)
		{
			ClassName = className;
			PropertyData = propData;
		}

		/// </summary>
		/// <summary>
		/// Creates a new instance of the RemoteInvocationException class.
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
			ClassName = info.GetString(GoreRemotingClassNameKey);
			PropertyData = (IReadOnlyDictionary<string, string>)info.GetValue(GoreRemotingPropertyDataKey, typeof(IReadOnlyDictionary<string, string>));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue(GoreRemotingClassNameKey, ClassName);
			info.AddValue(GoreRemotingPropertyDataKey, PropertyData);
		}
	}



}
