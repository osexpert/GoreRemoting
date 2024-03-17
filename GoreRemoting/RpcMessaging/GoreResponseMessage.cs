using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting.RpcMessaging
{

	public class GoreResponseMessage : IGorializer
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

		/// <summary>
		/// Gets or sets the type of the message.
		/// </summary>
		public ResponseType ResponseType { get; private set; }

		public MethodResultMessage MethodResult { get; private set; }

		public DelegateCallMessage DelegateCall { get; private set; }

		public void Deserialize(GoreBinaryReader r)
		{
			ResponseType = (ResponseType)r.ReadByte();

			if (ResponseType == ResponseType.DelegateCall)
				DelegateCall = new DelegateCallMessage(r);
			else if (ResponseType == ResponseType.MethodResult)
				MethodResult = new MethodResultMessage(r);
			else
				throw new NotImplementedException();
		}

		public void Deserialize(Stack<object?> st)
		{
			if (ResponseType == ResponseType.DelegateCall)
				DelegateCall.Deserialize(st);
			else if (ResponseType == ResponseType.MethodResult)
				MethodResult.Deserialize(st);
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
			else
				throw new NotImplementedException();
		}

		internal static GoreResponseMessage Deserialize(Stream s, ResponseType mType,
			string serviceName, string methodName, MethodInfo method,
			ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			if (mType == ResponseType.MethodResult)
				return new GoreResponseMessage(
					Gorializer.GoreDeserialize<MethodResultMessage>(s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
			else if (mType == ResponseType.DelegateCall)
				return new GoreResponseMessage(
					Gorializer.GoreDeserialize<DelegateCallMessage>(s, method, serializer, compressor), serviceName, methodName, serializer, compressor);
			else
				throw new Exception();
		}

		internal void Serialize(Stream s)
		{
			if (ResponseType == ResponseType.MethodResult)
				Gorializer.GoreSerialize(s, MethodResult, Serializer, Compressor);
			else if (ResponseType == ResponseType.DelegateCall)
				Gorializer.GoreSerialize(s, DelegateCall, Serializer, Compressor);
			else
				throw new Exception();
		}
	}


	public enum ResponseType
	{
		//MethodCall = 1,
		//DelegateResult = 2

		/// <summary>
		/// Result
		/// </summary>
		MethodResult = 3,
		/// <summary>
		/// Delegate
		/// </summary>
		DelegateCall = 4,
	}

}
