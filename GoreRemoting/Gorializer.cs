using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GoreRemoting
{
	public interface IGorializer
	{
		void Serialize(GoreBinaryWriter w, Stack<object> st);
		void Deserialize(GoreBinaryReader r);
		void Deserialize(Stack<object> st);
	}

	public static class PooledMemoryStream
	{
		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public static MemoryStream GetStream() => _manager.GetStream();
	}

	internal class Gorializer
	{

		public static void GoreSerialize(Stream ms, IGorializer data, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			//using var ms = PooledMemoryStream.GetStream();

			var cs = GetCompressor(compressor, ms) ?? ms;
			try
			{
				var stack = new Stack<object>();

				using (var bw = new GoreBinaryWriter(cs, leaveOpen: true))
				{
					data.Serialize(bw, stack);
				}

				serializer.Serialize(cs, stack.ToArray());
			}
			finally
			{
				if (cs != ms)
					cs.Dispose();
			}
			  
			//return ms.ToArray();
		}

		private static Stream GetCompressor(ICompressionProvider compressor, Stream ms)
		{
			if (compressor != null)
				return compressor.CreateCompressionStream(ms, System.IO.Compression.CompressionLevel.Fastest);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}

		public static T GoreDeserialize<T>(Stream ms, ISerializerAdapter serializer, ICompressionProvider compressor) where T : IGorializer, new()
		{
//			using var ms = new MemoryStream(data);

			var res = new T();

			var ds = GetDecompressor(compressor, ms) ?? ms;
			try
			{
				using (var br = new GoreBinaryReader(ds, leaveOpen: true))
				{
					res.Deserialize(br);
				}

				var arr = serializer.Deserialize(ds);
				res.Deserialize(new Stack<object>(arr));
			}
			finally
			{
				if (ds != ms)
					ds.Dispose();
			}

			return res;
		}

		private static Stream GetDecompressor(ICompressionProvider compressor, Stream ms)
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

        public GoreBinaryWriter(Stream outp, bool leaveOpen = false) : base(outp, _utf8NoBom, leaveOpen)
        {
			
        }

		public new void Write7BitEncodedInt(int i) => base.Write7BitEncodedInt(i);
	}

	public class GoreBinaryReader : BinaryReader
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

        public GoreBinaryReader(Stream inp, bool leaveOpen = false) : base(inp, _utf8NoBom, leaveOpen)
        {
        }

		public new int Read7BitEncodedInt() => base.Read7BitEncodedInt();

	}


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
