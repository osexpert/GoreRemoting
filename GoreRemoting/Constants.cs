using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting
{
	public static class Constants
	{

		public const string HeaderPrefix = "gore-";
		public const string TokenHeaderKey = "gore-token";
		public const string SessionIdHeaderKey = "gore-session-id";
		public const string SerializerHeaderKey = "gore-serializer";

//		internal const string UserAgentHeaderKey = "user-agent";
//		internal const string DotnetClientAgentStart = "grpc-dotnet/";
//		internal const string NativeClientAgentStart = "grpc-csharp/";
	}


	public static class TypeFormatter
	{
		public static Func<string, Type> ParseType { get; set; } = _ParseType;

		public static Func<Type, string> FormatType { get; set; } = _FormatType;

		private static string _FormatType(Type type)
		{
			// Why not use full asm name? type.Assembly.FullName (then it would be same as assemblyqualifyedname?)
			// Or maybe the simple name make most sense...
			//return type.FullName + "," + type.Assembly.GetName().Name;
			return TypeShortener.GetShortType(type);
		}

		private static Type _ParseType(string s)
		{
			return Type.GetType(s);
		}
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
