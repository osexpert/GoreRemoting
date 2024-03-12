namespace GoreRemoting
{
	public static class Constants
	{

		public const string HeaderPrefix = "gore-";
		public const string TokenHeaderKey = "gore-token";
		public const string SessionIdHeaderKey = "gore-session-id";

		/// <summary>
		/// v2: 
		/// - removed type name infos (security, now uses target method types)
		/// - changed call context to string values
		/// 
		/// </summary>
		internal static readonly byte SerializationVersion = 2;


		//	public const string SerializerHeaderKey = "gore-serializer";
		//		public const string CompressorHeaderKey = "gore-compressor";

		//		internal const string UserAgentHeaderKey = "user-agent";
		//		internal const string DotnetClientAgentStart = "grpc-dotnet/";
		//		internal const string NativeClientAgentStart = "grpc-csharp/";
	}



}
