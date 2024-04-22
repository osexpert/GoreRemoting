namespace GoreRemoting
{
	public static class Constants
	{

		public const string HeaderPrefix = "gore-";
		public const string TokenHeaderKey = "gore-token";
		public const string SessionIdHeaderKey = "gore-session-id";

		public const string GrpcServiceName = "GoreRemoting";

		/// <summary>
		/// v2: 
		/// - removed type name infos (security, now uses target method types)
		/// - changed call context to string values
		/// v3:
		/// - changed a lot...not pushing all args. Adding ParameterName\Position?
		/// - Add result types (kind).
		/// v4:
		/// - include execption map in gorelizer
		/// - move request type to top
		/// v5:
		/// - if only one type, no need to make generic Args (except for protobuf)
		/// </summary>
		internal static readonly byte SerializationVersion = 5;


		//	public const string SerializerHeaderKey = "gore-serializer";
		//		public const string CompressorHeaderKey = "gore-compressor";

		//		internal const string UserAgentHeaderKey = "user-agent";
		//		internal const string DotnetClientAgentStart = "grpc-dotnet/";
		//		internal const string NativeClientAgentStart = "grpc-csharp/";
	}



}
