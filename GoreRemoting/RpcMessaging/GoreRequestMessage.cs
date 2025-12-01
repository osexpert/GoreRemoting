using System.Reflection;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting.RpcMessaging;

public class GoreRequestMessage
{
	// only used on deserialize
	internal MethodInfo Method { get; set;  }

	internal string ServiceName { get; }
	internal string MethodName { get; }

	internal AsyncEnumCallResultMessage AsyncEnumCallResultMessage { get; }
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

	public GoreRequestMessage(AsyncEnumCallResultMessage aem, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		AsyncEnumCallResultMessage = aem;
		RequestType = RequestType.AsyncEnumCallResult;
		Serializer = serializer;
		Compressor = compressor;
		ServiceName = serviceName;
		MethodName = methodName;
	}

	public static GoreRequestMessage Deserialize(IRemotingParty r, Stream s, RequestType mType, string serviceName, string methodName,
		MethodInfo method, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		GoreRequestMessage res;
		if (mType == RequestType.DelegateResult)
			res = new GoreRequestMessage(GoreSerializer.Deserialize<DelegateResultMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else if (mType == RequestType.MethodCall)
			res = new GoreRequestMessage(GoreSerializer.Deserialize<MethodCallMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else if (mType == RequestType.AsyncEnumCallResult)
			res = new GoreRequestMessage(GoreSerializer.Deserialize<AsyncEnumCallResultMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else
			throw new Exception();

		res.Method = method;
		return res;
	}

	internal void Serialize(IRemotingParty r, Stream s, MethodInfo method)
	{
		if (RequestType == RequestType.DelegateResult)
			GoreSerializer.Serialize(r, s, method, DelegateResultMessage, Serializer, Compressor);
		else if (RequestType == RequestType.MethodCall)
			GoreSerializer.Serialize(r, s, method, MethodCallMessage, Serializer, Compressor);
		else if (RequestType == RequestType.AsyncEnumCallResult)
			GoreSerializer.Serialize(r, s, method, AsyncEnumCallResultMessage, Serializer, Compressor);
		else
			throw new Exception();

	}
}

public enum RequestType
{
	MethodCall = 1,
	DelegateResult = 2,

	//ResponseType.MethodResult = 3,
	//ResponseType.DelegateCall = 4,
	//ResponseType.AsyncEnumCall = 5,

	AsyncEnumCallResult = 6,

	//ResponseType.AsyncEnumReturnResult = 7
}
