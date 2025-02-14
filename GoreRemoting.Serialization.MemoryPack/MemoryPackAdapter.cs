using GoreRemoting.Serialization.MemoryPack.ArgTypes;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	public class MemoryPackAdapter : ISerializerAdapter
	{
		public string Name => "MemoryPack";

		public MemoryPackSerializerOptions? Options { get; }

		public MemoryPackAdapter() : this(null)
		{ }

		public MemoryPackAdapter(MemoryPackSerializerOptions? options)
		{
			Options = options;
		}

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			if (types.Length > 1)
			{
				var t = GetArgsType(types);
				var args = (IArgs)(Activator.CreateInstance(t) ?? throw new Exception($"Activator.CreateInstance returned null for type {t}"));
				args.Set(graph);
				object o = args;
				MemoryPackSerializer.SerializeAsync(t, stream, o, Options).GetResult();
			}
			else
			{
				MemoryPackSerializer.SerializeAsync(types[0], stream, graph[0], Options).GetResult();
			}
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			if (types.Length > 1)
			{
				var t = GetArgsType(types);
				var res = (IArgs)MemoryPackSerializer.DeserializeAsync(t, stream, Options).GetResult()!;
				return res.Get();
			}
			else
			{
				return new[] { MemoryPackSerializer.DeserializeAsync(types[0], stream, Options).GetResult() };
			}
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
