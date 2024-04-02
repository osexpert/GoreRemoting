// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GoreRemoting
{

	public static class ExceptionSerialization
	{
		public static ExceptionStrategy ExceptionStrategy => ExceptionStrategy.Clone;

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
				ExceptionStrategy.Clone => ExceptionSerialization.RestoreAsOriginalException(dict),
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
		/// Uses ISerializable.GetObjectData\ctor(SerializationInfo, StreamingContext).
		/// </summary>
		Clone = 1,
		/// <summary>
		/// Always type RemoteInvocationException.
		/// Uses ISerializable.GetObjectData\ctor(SerializationInfo, StreamingContext).
		/// </summary>
		RemoteInvocationException = 2
	}
}
