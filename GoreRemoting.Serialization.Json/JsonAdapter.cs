using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExtremeJsonEncoders;
using GoreRemoting.Serialization.Json.ArgTypes;
#if NET6_0_OR_GREATER
using TupleAsJsonArray;
#endif

namespace GoreRemoting.Serialization.Json
{
	public class JsonAdapter : ISerializerAdapter
	{
		public string Name => "Json";

		public JsonSerializerOptions Options { get; } = CreateDefaultOptions();

		private static JsonSerializerOptions CreateDefaultOptions()
		{
			return new JsonSerializerOptions()
			{
				// Match default behaviour of BF, Messagepack, Memorypack, Protobuf.
				IncludeFields = true,

				ReferenceHandler = ReferenceHandler.Preserve,
				Converters =
				{
#if NET6_0_OR_GREATER
					new TupleConverterFactory()
#endif
				},
				Encoder = MinimalJsonEncoder.Shared
			};
		}

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			Dictionary<int, byte[]> byteArrays = new();
			object?[] objects = new object[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];
				if (obj is byte[] bs)
				{
					byteArrays.Add(i, bs);
				}
				else
				{
					objects[i] = obj;
				}
			}

			var bw = new GoreBinaryWriter(stream);
			bw.WriteVarInt(byteArrays.Count);
			foreach (var byteArray in byteArrays)
			{
				bw.WriteVarInt(byteArray.Key);
				bw.WriteVarInt(byteArray.Value.Length);
				bw.Write(byteArray.Value);
			}

			if (types.Length > 1)
			{
				Type t = GetArgsType(types);
				var ins = (IArgs)Activator.CreateInstance(t)!;
				ins.Set(objects);
				object o = ins;
				JsonSerializer.Serialize(stream, o, Options);
			}
			else
			{
				JsonSerializer.Serialize(stream, objects[0], Options);
			}
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			Dictionary<int, byte[]> byteArrays = new();

			var br = new GoreBinaryReader(stream);
			var bCnt = br.ReadVarInt();
			while (bCnt-- > 0)
			{
				var idx = br.ReadVarInt();
				var byteLen = br.ReadVarInt();
				var bytes = br.ReadBytes(byteLen);
				byteArrays.Add(idx, bytes);
			}

			object?[] res;

			if (types.Length > 1)
			{
				Type t = GetArgsType(types);
				var objects = (IArgs)JsonSerializer.Deserialize(stream, t, Options)!;
				res = objects.Get();
			}
			else
			{
				res = new[] { JsonSerializer.Deserialize(stream, types[0], Options) };
			}

			foreach (var kv in byteArrays)
				res[kv.Key] = kv.Value;

			return res;
		}

		private static Type GetArgsType(Type[] types)
		{
			var type = types.Length switch
			{
				1 => typeof(Args<>).MakeGenericType(types),
				2 => typeof(Args<,>).MakeGenericType(types),
				3 => typeof(Args<,,>).MakeGenericType(types),
				4 => typeof(Args<,,,>).MakeGenericType(types),
				5 => typeof(Args<,,,,>).MakeGenericType(types),
				6 => typeof(Args<,,,,,>).MakeGenericType(types),
				7 => typeof(Args<,,,,,,>).MakeGenericType(types),
				8 => typeof(Args<,,,,,,,>).MakeGenericType(types),
				9 => typeof(Args<,,,,,,,,>).MakeGenericType(types),
				10 => typeof(Args<,,,,,,,,,>).MakeGenericType(types),
				11 => typeof(Args<,,,,,,,,,,>).MakeGenericType(types),
				12 => typeof(Args<,,,,,,,,,,,>).MakeGenericType(types),
				13 => typeof(Args<,,,,,,,,,,,,>).MakeGenericType(types),
				14 => typeof(Args<,,,,,,,,,,,,,>).MakeGenericType(types),
				15 => typeof(Args<,,,,,,,,,,,,,,>).MakeGenericType(types),
				16 => typeof(Args<,,,,,,,,,,,,,,,>).MakeGenericType(types),
				17 => typeof(Args<,,,,,,,,,,,,,,,,>).MakeGenericType(types),
				18 => typeof(Args<,,,,,,,,,,,,,,,,,>).MakeGenericType(types),
				19 => typeof(Args<,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),
				20 => typeof(Args<,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),
				_ => throw new NotImplementedException("Too many arguments")
			};
			return type;
		}
	}
}
