﻿using System.Text;
using GoreRemoting.Serialization.MessagePack.ArgTypes;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		public MessagePackSerializerOptions? Options { get; set; } = CreateDefaultOptions();

		private static MessagePackSerializerOptions CreateDefaultOptions()
		{
			return new MessagePackSerializerOptions(
				CompositeResolver.Create(
					NativeDateTimeResolver.Instance,
					MessagePackSerializerOptions.Standard.Resolver)
					);
		}

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			var t = GetArgsType(types);
			var args = (IArgs)Activator.CreateInstance(t);
			args.Set(graph);
			object o = args;

			MessagePackSerializer.Serialize(t, stream, o, Options);
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			var t = GetArgsType(types);
			var res = (IArgs)MessagePackSerializer.Deserialize(t, stream, Options)!;
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
