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
	internal interface IGorializer
	{
		void Serialize(GoreBinaryWriter w, Stack<object> st);
		void Deserialize(GoreBinaryReader r);
		void Deserialize(Stack<object> st);
	}


	internal class Gorializer
	{
		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public static byte[] GoreSerialize(IGorializer data, ISerializerAdapter serializer, ICompressionProvider compressor)
		{
			using var ms = _manager.GetStream();

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
			  
			return ms.ToArray();
		}

		private static Stream GetCompressor(ICompressionProvider compressor, MemoryStream ms)
		{
			if (compressor != null)
				return compressor.CreateCompressionStream(ms, System.IO.Compression.CompressionLevel.Fastest);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}

		public static T GoreDeserialize<T>(byte[] data, ISerializerAdapter serializer, ICompressionProvider compressor) where T : IGorializer, new()
		{
			//using var ms = _manager.GetStream(data);
			using var ms = new MemoryStream(data);

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

		private static Stream GetDecompressor(ICompressionProvider compressor, MemoryStream ms)
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

	/// <summary>
	/// The point? This will not dispose inner stream when disposed itself.
	/// </summary>
	//class NonDisposablePassthruStream : Stream
	//{
	//	Stream _s;

	//	public NonDisposablePassthruStream(Stream s)
	//	{
	//		_s = s;
	//	}

	//	public override bool CanRead => _s.CanRead;

	//	public override bool CanSeek => _s.CanSeek;

	//	public override bool CanWrite => _s.CanWrite;

	//	public override long Length => _s.Length;

	//	public override long Position { get => _s.Position; set => _s.Position = value; }

	//	public override void Flush()
	//	{
	//		_s.Flush();
	//	}

	//	public override int Read(byte[] buffer, int offset, int count)
	//	{
	//		return _s.Read(buffer, offset, count);
	//	}

	//	public override long Seek(long offset, SeekOrigin origin)
	//	{
	//		return _s.Seek(offset, origin);
	//	}

	//	public override void SetLength(long value)
	//	{
	//		_s.SetLength(value);
	//	}

	//	public override void Write(byte[] buffer, int offset, int count)
	//	{
	//		_s.Write(buffer, offset, count);
	//	}

	//	protected override void Dispose(bool disposing)
	//	{
	//		// skip
	//	}
	//}
}
