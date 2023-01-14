using System;
using System.Collections.Generic;
using System.Text;

namespace GrpcRemoting
{
	public static class Constants
	{
		internal const byte ClientHangupByte = 0x42;
	
		public const string HeaderPrefix = "grem-";
		public const string SessionIdHeaderKey = "grem-session-id";
		internal const string SerializerHeaderKey = "grem-serializer";

		internal const string UserAgentHeaderKey = "user-agent";
		internal const string DotnetClientAgentStart = "grpc-dotnet/";
		internal const string NativeClientAgentStart = "grpc-csharp/";
	}
}
