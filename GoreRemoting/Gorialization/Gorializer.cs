﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting
{
	public interface IGorializer
	{
		void Serialize(GoreBinaryWriter w, Stack<object?> st);
		void Deserialize(GoreBinaryReader r);
		void Deserialize(Stack<object?> st);
	}

	internal class Gorializer
	{

		public static void GoreSerialize(Stream ms, IGorializer data, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			//using var ms = PooledMemoryStream.GetStream();

			var cs = GetCompressor(compressor, ms) ?? ms;
			try
			{
				var stack = new Stack<object?>();

				var bw = new GoreBinaryWriter(cs);
				data.Serialize(bw, stack);

				serializer.Serialize(cs, stack.ToArray());
			}
			finally
			{
				if (cs != ms)
					cs.Dispose();
			}

			//return ms.ToArray();
		}

		private static Stream? GetCompressor(ICompressionProvider? compressor, Stream ms)
		{
			if (compressor != null)
				return compressor.CreateCompressionStream(ms, System.IO.Compression.CompressionLevel.Fastest);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}

		public static T GoreDeserialize<T>(Stream ms, ISerializerAdapter serializer, ICompressionProvider? compressor) where T : IGorializer, new()
		{
			//			using var ms = new MemoryStream(data);

			var res = new T();

			var ds = GetDecompressor(compressor, ms) ?? ms;
			try
			{
				var br = new GoreBinaryReader(ds);
				res.Deserialize(br);

				var arr = serializer.Deserialize(ds);
				res.Deserialize(new Stack<object?>(arr));
			}
			finally
			{
				if (ds != ms)
					ds.Dispose();
			}

			return res;
		}

		private static Stream? GetDecompressor(ICompressionProvider? compressor, Stream ms)
		{
			if (compressor != null)
				return compressor.CreateDecompressionStream(ms);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}
	}

	public class GoreBinaryWriter : BinaryWriter
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryWriter(Stream outp) : base(outp, _utf8NoBom, leaveOpen: true)
		{

		}

		public void WriteVarInt(int i) => base.Write7BitEncodedInt(i);

		public void Write(Guid g) => Write(g.ToByteArray());
	}

	public class GoreBinaryReader : BinaryReader
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryReader(Stream inp) : base(inp, _utf8NoBom, leaveOpen: true)
		{
		}

		public int ReadVarInt() => base.Read7BitEncodedInt();

		public Guid ReadGuid() => new Guid(ReadBytes(16));
	}

	[System.AttributeUsage(System.AttributeTargets.Interface | AttributeTargets.Method)]
	public class SerializerAttribute : Attribute
	{
		public Type Serializer { get; }

		public SerializerAttribute(Type t)
		{
			if (!typeof(ISerializerAdapter).IsAssignableFrom(t))
				throw new Exception("Not ISerializerAdapter");

			Serializer = t;
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Interface | AttributeTargets.Method)]
	public class CompressorAttribute : Attribute
	{
		public Type Compressor { get; }

		public CompressorAttribute(Type t)
		{
			if (!typeof(ICompressionProvider).IsAssignableFrom(t))
				throw new Exception("Not ICompressionProvider");

			Compressor = t;
		}
	}

}
