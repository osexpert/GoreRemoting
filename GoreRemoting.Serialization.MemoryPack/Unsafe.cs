using MemoryPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GoreRemoting.Serialization.MemoryPack
{
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

		// see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
		static readonly Regex AssemblyNameVersionSelectorRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=[\w-]+, PublicKeyToken=(?:null|[a-f0-9]{16})", RegexOptions.Compiled);
		static readonly ConcurrentDictionary<Type, string> typeNameCache = new ConcurrentDictionary<Type, string>();

		public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object? value)
		{
			if (value == null)
			{
				writer.WriteNullObjectHeader();
				return;
			}

			var type = value.GetType();
			string typeName = GetShortType(type);

			writer.WriteObjectHeader(2);
			writer.WriteString(typeName);
			writer.WriteValue(type, value);
		}

		public static string GetShortType(Type type)
		{
			if (!typeNameCache.TryGetValue(type, out var typeName))
			{
				var full = type.AssemblyQualifiedName!;

				var shortened = AssemblyNameVersionSelectorRegex.Replace(full, string.Empty);
				if (Type.GetType(shortened, false) == null)
				{
					// if type cannot be found with shortened name - use full name
					shortened = full;
				}

				typeNameCache[type] = shortened;
				typeName = shortened;
			}

			return typeName;
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
