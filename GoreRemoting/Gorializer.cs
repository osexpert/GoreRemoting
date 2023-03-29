using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
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

		public static byte[] GoreSerialize(IGorializer data, ISerializerAdapter serializer)
		{
			using var ms = _manager.GetStream();

			var stack = new Stack<object>();

			using var bw = new GoreBinaryWriter(ms, leaveOpen: true);
			data.Serialize(bw, stack);

			serializer.Serialize(ms, stack.ToArray());

			return ms.ToArray();
		}

		public static T GoreDeserialize<T>(byte[] data, ISerializerAdapter serializer) where T : IGorializer, new()
		{
			using var ms = _manager.GetStream(data);
			using var br = new GoreBinaryReader(ms, leaveOpen: true);

			var res = new T();
			res.Deserialize(br);

			var arr = serializer.Deserialize(ms);

			res.Deserialize(new Stack<object>(arr));

			return res;
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
}
