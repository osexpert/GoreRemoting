using System.Reflection;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting.RpcMessaging;

public class GoreResponseMessage : IGoreSerializable
{
	internal string ServiceName { get; }
	internal string MethodName { get; }

	internal ISerializerAdapter Serializer { get; }
	internal ICompressionProvider? Compressor { get; }

	public GoreResponseMessage(DelegateCallMessage callMsg, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		DelegateCall = callMsg;
		ResponseType = ResponseType.DelegateCall;
		Serializer = serializer;
		Compressor = compressor;
		ServiceName = serviceName;
		MethodName = methodName;
	}

	public GoreResponseMessage(MethodResultMessage resultMessage, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		MethodResult = resultMessage;
		ResponseType = ResponseType.MethodResult;
		Serializer = serializer;
		Compressor = compressor;
		ServiceName = serviceName;
		MethodName = methodName;
	}

	public GoreResponseMessage(AsyncEnumCallMessage resultMessage, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		AsyncEnumCall = resultMessage;
		ResponseType = ResponseType.AsyncEnumCall;
		Serializer = serializer;
		Compressor = compressor;
		ServiceName = serviceName;
		MethodName = methodName;
	}

	public GoreResponseMessage(AsyncEnumReturnResultMessage returnResultMessage, string serviceName, string methodName, ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		AsyncEnumReturnResult = returnResultMessage;
		ResponseType = ResponseType.AsyncEnumReturnResult;
		Serializer = serializer;
		Compressor = compressor;
		ServiceName = serviceName;
		MethodName = methodName;
	}

	/// <summary>
	/// Gets or sets the type of the message.
	/// </summary>
	public ResponseType ResponseType { get; private set; }

	public MethodResultMessage MethodResult { get; private set; }

	public DelegateCallMessage DelegateCall { get; private set; }

	public AsyncEnumCallMessage AsyncEnumCall { get; private set; }

	public AsyncEnumReturnResultMessage AsyncEnumReturnResult { get; private set; }


	public void Deserialize(GoreBinaryReader r)
	{
		ResponseType = (ResponseType)r.ReadByte();

		if (ResponseType == ResponseType.DelegateCall)
			DelegateCall = new DelegateCallMessage(r);
		else if (ResponseType == ResponseType.MethodResult)
			MethodResult = new MethodResultMessage(r);
		else if (ResponseType == ResponseType.AsyncEnumCall)
			AsyncEnumCall = new AsyncEnumCallMessage(r);
		else if (ResponseType == ResponseType.AsyncEnumReturnResult)
			AsyncEnumReturnResult = new AsyncEnumReturnResultMessage(r);
		else
			throw new NotImplementedException();
	}

	public void Deserialize(Stack<object?> st)
	{
		if (ResponseType == ResponseType.DelegateCall)
			DelegateCall.Deserialize(st);
		else if (ResponseType == ResponseType.MethodResult)
			MethodResult.Deserialize(st);
		else if (ResponseType == ResponseType.AsyncEnumCall)
			AsyncEnumCall.Deserialize(st);
		else if (ResponseType == ResponseType.AsyncEnumReturnResult)
			AsyncEnumReturnResult.Deserialize(st);
		else
			throw new NotImplementedException();
	}

	public void Serialize(GoreBinaryWriter w, Stack<object?> st)
	{
		w.Write((byte)ResponseType);

		if (ResponseType == ResponseType.DelegateCall)
			DelegateCall.Serialize(w, st);
		else if (ResponseType == ResponseType.MethodResult)
			MethodResult.Serialize(w, st);
		else if (ResponseType == ResponseType.AsyncEnumCall)
			AsyncEnumCall.Serialize(w, st);
		else if (ResponseType == ResponseType.AsyncEnumReturnResult)
			AsyncEnumReturnResult.Serialize(w, st);
		else
			throw new NotImplementedException();
	}

	internal static GoreResponseMessage Deserialize(IRemotingParty r, Stream s, ResponseType mType,
		string serviceName, string methodName, MethodInfo method,
		ISerializerAdapter serializer, ICompressionProvider? compressor)
	{
		if (mType == ResponseType.MethodResult)
			return new GoreResponseMessage(
				GoreSerializer.Deserialize<MethodResultMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else if (mType == ResponseType.DelegateCall)
			return new GoreResponseMessage(
				GoreSerializer.Deserialize<DelegateCallMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else if (mType == ResponseType.AsyncEnumCall)
			return new GoreResponseMessage(
				GoreSerializer.Deserialize<AsyncEnumCallMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else if (mType == ResponseType.AsyncEnumReturnResult)
			return new GoreResponseMessage(
				GoreSerializer.Deserialize<AsyncEnumReturnResultMessage>(r, s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
		else
			throw new Exception();
	}

	internal void Serialize(IRemotingParty r, Stream s, MethodInfo method)
	{
		if (ResponseType == ResponseType.MethodResult)
			GoreSerializer.Serialize(r, s, method, MethodResult, Serializer, Compressor);
		else if (ResponseType == ResponseType.DelegateCall)
			GoreSerializer.Serialize(r, s, method, DelegateCall, Serializer, Compressor);
		else if (ResponseType == ResponseType.AsyncEnumCall)
			GoreSerializer.Serialize(r, s, method, AsyncEnumCall, Serializer, Compressor);
		else if (ResponseType == ResponseType.AsyncEnumReturnResult)
			GoreSerializer.Serialize(r, s, method, AsyncEnumReturnResult, Serializer, Compressor);
		else
			throw new Exception();
	}
}


public enum ResponseType
{
	//RequestType.MethodCall = 1,
	//RequestType.DelegateResult = 2

	/// <summary>
	/// Result
	/// </summary>
	MethodResult = 3,
	/// <summary>
	/// Delegate
	/// </summary>
	DelegateCall = 4,


	AsyncEnumCall = 5,

	//RequestType.AsyncEnumCallResult = 6

	AsyncEnumReturnResult = 7,
}
