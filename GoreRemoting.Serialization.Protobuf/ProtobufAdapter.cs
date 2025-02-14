using GoreRemoting.Serialization.Protobuf.ArgTypes;
using GoreRemoting.Serialization.Protobuf.Surrogates;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GoreRemoting.Serialization.Protobuf
{
	public class ProtobufAdapter : ISerializerAdapter
	{
		public string Name => "Protobuf";

		private static object _lock = new();

		public ProtobufAdapter() : this(GetDefaultSurrogates())
		{ }

		public ProtobufAdapter(IEnumerable<Surrogate>? surrogates)
		{
			if (surrogates != null && surrogates.Any())
			{
				foreach (var surrogate in surrogates)
				{
					lock (_lock)
					{
						if (!RuntimeTypeModel.Default.IsDefined(surrogate.Type))
							RuntimeTypeModel.Default.Add(surrogate.Type, false).SetSurrogate(surrogate.SurrogateType);
					}
				}
			}
		}

		public class Surrogate
		{
			/// <summary>
			/// The type handled by the [ProtoConverter]'s
			/// </summary>
			public Type Type { get; private set; }

			/// <summary>
			/// The class that has a [ProtoContract]
			/// </summary>
			public Type SurrogateType { get; private set; }

			public Surrogate(Type type, Type surrogateType)
			{
				Type = type;
				SurrogateType = surrogateType;
			}
		}

		public static Surrogate[] GetDefaultSurrogates()
		{
			return
			[
				new Surrogate(typeof(Version), typeof(VersionSurrogate)),
				new Surrogate(typeof(DateTimeOffset), typeof(DateTimeOffsetSurrogate))
			];
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
