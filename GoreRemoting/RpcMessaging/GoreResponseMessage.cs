using GoreRemoting.Serialization;
using Grpc.Net.Compression;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoreRemoting.RpcMessaging
{

	public class GoreResponseMessage : IGorializer
	{
		internal ISerializerAdapter Serializer { get; }
		internal ICompressionProvider Compressor { get; }

		public GoreResponseMessage(DelegateCallMessage callMsg, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			DelegateCall = callMsg;
			ResponseType = ResponseType.DelegateCall;
			Serializer = serializer;
			Compressor = compressor;
		}

		public GoreResponseMessage(MethodResultMessage resultMessage, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			MethodResult = resultMessage;
			ResponseType = ResponseType.MethodResult;
			Serializer = serializer;
			Compressor = compressor;
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

		public void Deserialize(Stack<object> st)
		{
			if (ResponseType == ResponseType.DelegateCall)
				DelegateCall.Deserialize(st);
			else if (ResponseType == ResponseType.MethodResult)
				MethodResult.Deserialize(st);
			else
				throw new NotImplementedException();
		}

		public void Serialize(GoreBinaryWriter w, Stack<object> st)
		{
			w.Write((byte)ResponseType);

			if (ResponseType == ResponseType.DelegateCall)
				DelegateCall.Serialize(w, st);
			else if (ResponseType == ResponseType.MethodResult)
				MethodResult.Serialize(w, st);
			else
				throw new NotImplementedException();
		}

		internal static GoreResponseMessage Deserialize(Stream s, ResponseType mType, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			if (mType == ResponseType.MethodResult)
				return new GoreResponseMessage(
					Gorializer.GoreDeserialize<MethodResultMessage>(s, serializer, compressor), serializer, compressor);
			else if (mType == ResponseType.DelegateCall)
				return new GoreResponseMessage(
					Gorializer.GoreDeserialize<DelegateCallMessage>(s, serializer, compressor), serializer, compressor);
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
		/// <summary>
		/// Result
		/// </summary>
		MethodResult = 1,
		/// <summary>
		/// Delegate
		/// </summary>
		DelegateCall = 2,
	}

}
