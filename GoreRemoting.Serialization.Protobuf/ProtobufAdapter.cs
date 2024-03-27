using System;
using System.Collections.Generic;
using System.IO;
using GoreRemoting.Serialization.Protobuf.ArgTypes;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GoreRemoting.Serialization.Protobuf
{
	public class ProtobufAdapter : ISerializerAdapter
	{
		public string Name => "Protobuf";

		private static object _lock = new();

		public ProtobufAdapter(bool addSurrogates = true)
		{
			if (addSurrogates)
			{
				lock (_lock)
				{
					if (!RuntimeTypeModel.Default.IsDefined(typeof(Version)))
						RuntimeTypeModel.Default.Add(typeof(Version), false).SetSurrogate(typeof(VersionSurrogate));

					if (!RuntimeTypeModel.Default.IsDefined(typeof(DateTimeOffset)))
						RuntimeTypeModel.Default.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate));
				}
			}
		}

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			var t = GetArgsType(types);
			var args = (IArgs)Activator.CreateInstance(t);
			args.Set(graph);
			object o = args;

			Serializer.Serialize(stream, o);
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			var t = GetArgsType(types);
			var res = (IArgs)Serializer.Deserialize(t, stream);
			return res.Get();
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
