﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting
{
	public static class Constants
	{

		public const string HeaderPrefix = "gore-";
		public const string TokenHeaderKey = "gore-token";
		public const string SessionIdHeaderKey = "gore-session-id";
		//	public const string SerializerHeaderKey = "gore-serializer";
		//		public const string CompressorHeaderKey = "gore-compressor";

		//		internal const string UserAgentHeaderKey = "user-agent";
		//		internal const string DotnetClientAgentStart = "grpc-dotnet/";
		//		internal const string NativeClientAgentStart = "grpc-csharp/";
	}




	public class StreamingFuncAttribute : Attribute
	{
		public StreamingFuncAttribute()
		{
		}
	}

	public class StreamingDoneException : Exception
	{
	}


}
