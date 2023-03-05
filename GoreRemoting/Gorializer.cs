using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GoreRemoting
{
	internal class Gorializer
	{
		public static byte[] GoreSerialize(IGorializer data, ISerializerAdapter serializer)
		{
			using var ms = new MemoryStream();

			var stack = new Stack<object>();

			using var bw = new System.IO.BinaryWriter(ms, new UTF8Encoding(false), leaveOpen: true);
			data.Serialize(bw, stack);

			serializer.Serialize<object[]>(ms, stack.ToArray());

			return ms.ToArray();
		}

		public static T GoreDeserialize<T>(byte[] data, ISerializerAdapter serializer) where T : IGorializer, new()
		{
			using var ms = new MemoryStream(data);
			using var br = new System.IO.BinaryReader(ms, new UTF8Encoding(false), leaveOpen: true);

			var res = new T();
			res.Deserialize(br);

			var arr = serializer.Deserialize<object[]>(ms);

			res.Deserialize(new Stack<object>(arr));

			return res;
		}
	
	}
}
