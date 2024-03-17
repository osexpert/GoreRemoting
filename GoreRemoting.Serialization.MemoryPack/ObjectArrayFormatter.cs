
using System;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	/// <summary>
	/// https://github.com/Cysharp/MemoryPack/issues/114
	/// </summary>
	internal sealed class ObjectArrayFormatterAttribute : MemoryPackCustomFormatterAttribute<ObjectArrayFormatter, object?[]>
	{
		public override ObjectArrayFormatter GetFormatter()
		{
			return ObjectArrayFormatter.Default;
		}
	}

	internal sealed class ObjectArrayFormatter : MemoryPackFormatter<object?[]>
	{
		internal static readonly ObjectArrayFormatter Default = new ObjectArrayFormatter();

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
				if (v == null)
				{
					writer.WriteNullObjectHeader();
				}
				else
				{
					var type = v.GetType();
					writer.WriteObjectHeader(1);
					writer.WriteValue(type, v);
				}
			}
		}

		public override void Deserialize(ref MemoryPackReader reader, scoped ref object?[]? value)
		{
			try
			{
				var types = MemoryPackAdapter._asyncLocalTypes.Value;
				if (types == null)
					throw new Exception("types are null");

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

				if (types.Length != length)
					throw new Exception("length mismatch");

				for (int i = 0; i < length; i++)
				{
					object? v = null;
					if (!reader.TryReadObjectHeader(out var count))
					{
						v = null;
					}
					else
					{
						if (count != 1) MemoryPackSerializationException.ThrowInvalidPropertyCount(1, count);
						var type = types[i];
						reader.ReadValue(type, ref v);
					}

					value[i] = v;
				}
			}
			finally
			{
				MemoryPackAdapter._asyncLocalTypes.Value = null;
			}
		}
	}
}
