using GoreRemoting.Serialization;
using Grpc.Net.Compression;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoreRemoting.RpcMessaging
{

	public class GoreRequestMessage
	{

		internal DelegateResultMessage DelegateResultMessage { get; }
		internal MethodCallMessage MethodCallMessage { get; }

		public RequestType RequestType { get; }

		internal ISerializerAdapter Serializer { get; }
		internal ICompressionProvider Compressor { get; }

        public GoreRequestMessage(DelegateResultMessage drm, ISerializerAdapter serializer, ICompressionProvider compressor)
        {
			DelegateResultMessage = drm;
			RequestType = RequestType.DelegateResult;
			Serializer = serializer;
			Compressor = compressor;
        }

		public GoreRequestMessage(MethodCallMessage mcm, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			MethodCallMessage = mcm;
			RequestType = RequestType.MethodCall;
			Serializer = serializer;
			Compressor = compressor;
		}

		public static GoreRequestMessage Deserialize(Stream s, RequestType mType, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			if (mType == RequestType.DelegateResult)
				return new GoreRequestMessage(Gorializer.GoreDeserialize<DelegateResultMessage>(s, serializer, compressor), serializer, compressor);
			else if (mType == RequestType.MethodCall)
				return new GoreRequestMessage(Gorializer.GoreDeserialize<MethodCallMessage>(s, serializer, compressor), serializer, compressor);
			else
				throw new Exception();
		}

		internal void Serialize(Stream s)
		{
			if (RequestType == RequestType.DelegateResult)
				Gorializer.GoreSerialize(s, DelegateResultMessage, Serializer, Compressor);
			else if (RequestType == RequestType.MethodCall)
				Gorializer.GoreSerialize(s, MethodCallMessage, Serializer, Compressor);
			else
				throw new Exception();

		}
	}

	public enum RequestType
	{
		MethodCall, 
		DelegateResult
	}

}
