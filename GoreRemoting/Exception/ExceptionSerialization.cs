// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using GoreRemoting.Serialization;

namespace GoreRemoting
{

	public static class ExceptionSerialization
	{
		public static ExceptionStrategy ExceptionStrategy => ExceptionStrategy.Keep;

		public static Exception RestoreAsOriginalException(Dictionary<string, string> dict)
		{
			return ExceptionConverter.ToException(dict);
		}

		public static Exception RestoreAsRemoteInvocationException(Dictionary<string, string> dict)
		{
			return ExceptionConverter.ReadRemoteInvocationException(dict);
		}

		public static Dictionary<string, string> GetSerializableExceptionDictionary(Exception ex)
		{
			return ExceptionConverter.ToDict(ex);
		}

		public static Exception RestoreSerializedExceptionDictionary(Dictionary<string, string> dict)
		{
			return ExceptionStrategy switch
			{
				ExceptionStrategy.Keep => ExceptionSerialization.RestoreAsOriginalException(dict),
				ExceptionStrategy.RemoteInvocationException => ExceptionSerialization.RestoreAsRemoteInvocationException(dict),
				_ => throw new NotImplementedException()
			};
		}
	}




	//public class ExceptionData
	//{
	//	public string FullStackTrace()
	//	{
	//		// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/exception.cs
	//		var ie = InnerException;
	//		if (string.IsNullOrEmpty(ie)) // protobuff: null becomes ""
	//		//if (ie == null)
	//			return StackTrace ?? string.Empty;
	//		else
	//			return " ---> " + ie + Environment.NewLine + "   " + "--- End of inner exception stack trace ---" + Environment.NewLine + StackTrace;
	//	}
	//}

	public enum ExceptionStrategy
	{
		/// <summary>
		/// Same type as original, but some pieces may be missing (best effort).
		/// </summary>
		Keep = 1,
		/// <summary>
		/// Always type RemoteInvocationException.
		/// </summary>
		RemoteInvocationException = 2
	}
}
