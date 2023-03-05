using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
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
		public static byte[] GoreSerialize(IGorializer data, ISerializerAdapter serializer)
		{
			using var ms = new MemoryStream();

			var stack = new Stack<object>();

			using var bw = new GoreBinaryWriter(ms, leaveOpen: true);
			data.Serialize(bw, stack);

			serializer.Serialize(ms, stack.ToArray());

			return ms.ToArray();
		}

		public static T GoreDeserialize<T>(byte[] data, ISerializerAdapter serializer) where T : IGorializer, new()
		{
			using var ms = new MemoryStream(data);
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
		static Encoding encu8nobom = new UTF8Encoding(false);

        public GoreBinaryWriter(Stream outp, bool leaveOpen = false) : base(outp, encu8nobom, leaveOpen)
        {
			
        }

		public new void Write7BitEncodedInt(int i) => base.Write7BitEncodedInt(i);
	}

	public class GoreBinaryReader : BinaryReader
	{
		static Encoding encu8nobom = new UTF8Encoding(false);

        public GoreBinaryReader(Stream inp, bool leaveOpen = false) : base(inp, encu8nobom, leaveOpen)
        {
        }

		public new int Read7BitEncodedInt() => base.Read7BitEncodedInt();

	}
}
