
using System;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	/// <summary>
	/// https://github.com/Cysharp/MemoryPack/issues/114
	/// </summary>
	public sealed class UnsafeObjectFormatterAttribute : MemoryPackCustomFormatterAttribute<UnsafeObjectFormatter, object>
	{
		public override UnsafeObjectFormatter GetFormatter()
		{
			return UnsafeObjectFormatter.Default;
		}
	}

	public sealed class UnsafeObjectArrayFormatterAttribute : MemoryPackCustomFormatterAttribute<UnsafeObjectArrayFormatter, object?[]>
	{
		public override UnsafeObjectArrayFormatter GetFormatter()
		{
			return UnsafeObjectArrayFormatter.Default;
		}
	}

	public sealed class UnsafeObjectFormatter : MemoryPackFormatter<object>
	{
		public static readonly UnsafeObjectFormatter Default = new UnsafeObjectFormatter();

		public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object? value)
		{
			if (value == null)
			{
				writer.WriteNullObjectHeader();
				return;
			}

			var type = value.GetType();
			string typeName = TypeShortener.GetShortType(type);

			writer.WriteObjectHeader(2);
			writer.WriteString(typeName);
			writer.WriteValue(type, value);
		}

		public override void Deserialize(ref MemoryPackReader reader, scoped ref object? value)
		{
			if (!reader.TryReadObjectHeader(out var count))
			{
				value = null;
				return;
			}

			if (count != 2) MemoryPackSerializationException.ThrowInvalidPropertyCount(2, count);

			var typeName = reader.ReadString();
			var type = Type.GetType(typeName!);
			reader.ReadValue(type!, ref value);
		}
	}

	public sealed class UnsafeObjectArrayFormatter : MemoryPackFormatter<object?[]>
	{
		public static readonly UnsafeObjectArrayFormatter Default = new UnsafeObjectArrayFormatter();

		public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object?[]? value)
		{
			if (value == null)
			{
				writer.WriteNullCollectionHeader();
				return;
			}

			writer.WriteCollectionHeader(value.Length);
			foreach (var item in value)
			{
				var v = item;
				UnsafeObjectFormatter.Default.Serialize(ref writer, ref v);
			}
		}

		public override void Deserialize(ref MemoryPackReader reader, scoped ref object?[]? value)
		{
			if (!reader.TryReadCollectionHeader(out var length))
			{
				value = null;
				return;
			}

			if (length == 0)
			{
				value = Array.Empty<object>();
				return;
			}

			if (value == null || value.Length != length)
			{
				value = new object[length];
			}

			for (int i = 0; i < length; i++)
			{
				object? v = null;
				UnsafeObjectFormatter.Default.Deserialize(ref reader, ref v);
				value[i] = v;
			}
		}
	}
}
