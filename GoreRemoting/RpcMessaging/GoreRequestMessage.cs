using System;
using System.IO;
using System.Reflection;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting.RpcMessaging
{

	public class GoreRequestMessage
	{
		// only used on deserialize
		internal MethodInfo Method { get; set;  }

		internal string ServiceName { get; }
		internal string MethodName { get; }

		internal DelegateResultMessage DelegateResultMessage { get; }
		internal MethodCallMessage MethodCallMessage { get; }

		public RequestType RequestType { get; }

		internal ISerializerAdapter Serializer { get; }
		internal ICompressionProvider? Compressor { get; }


		public GoreRequestMessage(DelegateResultMessage drm, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			DelegateResultMessage = drm;
			RequestType = RequestType.DelegateResult;
			Serializer = serializer;
			Compressor = compressor;
			ServiceName = serviceName;
			MethodName = methodName;
		}

		public GoreRequestMessage(MethodCallMessage mcm, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			MethodCallMessage = mcm;
			RequestType = RequestType.MethodCall;
			Serializer = serializer;
			Compressor = compressor;
			ServiceName = serviceName;
			MethodName = methodName;
		}

		public static GoreRequestMessage Deserialize(Stream s, RequestType mType, string serviceName, string methodName, MethodInfo method, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			GoreRequestMessage res;
			if (mType == RequestType.DelegateResult)
				res = new GoreRequestMessage(Gorializer.GoreDeserialize<DelegateResultMessage>(s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
			else if (mType == RequestType.MethodCall)
				res = new GoreRequestMessage(Gorializer.GoreDeserialize<MethodCallMessage>(s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
			else
				throw new Exception();

			res.Method = method;
			return res;
		}

		internal void Serialize(Stream s, MethodInfo method)
		{
			if (RequestType == RequestType.DelegateResult)
				Gorializer.GoreSerialize(s, method, DelegateResultMessage, Serializer, Compressor);
			else if (RequestType == RequestType.MethodCall)
				Gorializer.GoreSerialize(s, method, MethodCallMessage, Serializer, Compressor);
			else
				throw new Exception();

		}
	}

	public enum RequestType
	{
		MethodCall = 1,
		DelegateResult = 2
	}

}
