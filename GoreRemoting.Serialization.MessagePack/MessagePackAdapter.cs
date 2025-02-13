using GoreRemoting.Serialization.MessagePack.ArgTypes;
using MessagePack;
using MessagePack.Resolvers;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		public MessagePackSerializerOptions? Options { get; set; } = CreateDefaultOptions();

		public static MessagePackSerializerOptions CreateDefaultOptions()
		{
			return CreateDefaultOptions(new IFormatterResolver[]
			{
				NativeDateTimeResolver.Instance,
				StandardResolver.Instance
			});
		}

		public static MessagePackSerializerOptions CreateDefaultOptions(params IFormatterResolver[] resolvers)
		{
			var compositeResolver = CompositeResolver.Create(resolvers);
			return MessagePackSerializerOptions.Standard.WithResolver(new DedupingResolver(compositeResolver));
		}

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			if (types.Length > 1)
			{
				var t = GetArgsType(types);
				var args = (IArgs)Activator.CreateInstance(t);
				args.Set(graph);
				object o = args;
				MessagePackSerializer.Serialize(t, stream, o, Options);
			}
			else
			{
				MessagePackSerializer.Serialize(types[0], stream, graph[0], Options);
			}
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			if (types.Length > 1)
			{
				var t = GetArgsType(types);
				var res = (IArgs)MessagePackSerializer.Deserialize(t, stream, Options)!;
				return res.Get();
			}
			else
			{
				return new[] { MessagePackSerializer.Deserialize(types[0], stream, Options) };
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
