#if false
using MemoryPack;
using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GoreRemoting.Serialization.MemoryPack
{
	/// <summary>
	/// https://github.com/Cysharp/MemoryPack/issues/114
	/// </summary>
	public sealed class UnsafeObjectFormatterAttribute2 : MemoryPackCustomFormatterAttribute<UnsafeObjectFormatter2, object>
	{
		public override UnsafeObjectFormatter2 GetFormatter()
		{
			return UnsafeObjectFormatter2.Default;
		}
	}

	public sealed class UnsafeObjectArrayFormatterAttribute2 : MemoryPackCustomFormatterAttribute<UnsafeObjectArrayFormatter2, object?[]>
	{
		public override UnsafeObjectArrayFormatter2 GetFormatter()
		{
			return UnsafeObjectArrayFormatter2.Default;
		}
	}

	public sealed class UnsafeObjectFormatter2 : MemoryPackFormatter<object>
	{
		public static readonly UnsafeObjectFormatter2 Default = new UnsafeObjectFormatter2();

		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object? value)
		{
			if (value == null)
			{
				writer.WriteNullObjectHeader();
				return;
			}

			var type = value.GetType();
			//string typeName = TypeShortener.GetShortType(type);

			writer.WriteObjectHeader(2);
			//writer.WriteValue(GetTypeCode(type));

			// make a fake write to get the length
			using (var s = (RecyclableMemoryStream)_manager.GetStream())
			{
				var ss = s; // silence warning
				// WHY is ref used here???
				var w = new MemoryPackWriter<RecyclableMemoryStream>(ref ss, writer.OptionalState);
				w.WriteValue(type, value);
				w.Flush();

				// or use length?
				writer.WriteVarInt((int)s.Position);
				var sr = writer.GetSpanReference((int)s.Position);
				//Unsafe.WriteUnaligned(ref sr, s.ToArray());
				var from = s.GetBuffer().AsSpan();
				Unsafe.CopyBlock(ref sr, ref MemoryMarshal.GetReference(from), (uint)s.Position);
				writer.Advance((int)s.Position);
			}
		}

		//private byte GetTypeCode(Type type)
		//{
		//	var tc = Type.GetTypeCode(type);

		//	ValidateTypeCode(tc);

		//	return (byte)tc;
		//}

		//private void ValidateTypeCode(TypeCode tc)
		//{
		//	switch (tc)
		//	{
		//		case TypeCode.Boolean:
		//		case TypeCode.Byte:
		//		case TypeCode.Char:
		//		case TypeCode.DateTime:
		//		case TypeCode.Decimal:
		//		case TypeCode.Double:
		//		case TypeCode.Int16:
		//		case TypeCode.Int32:
		//		case TypeCode.Int64:
		//		case TypeCode.SByte:
		//		case TypeCode.Single:
		//		case TypeCode.String:
		//		case TypeCode.UInt16:
		//		case TypeCode.UInt32:
		//		case TypeCode.UInt64:
		//			return;
		//	}

		//	throw new Exception("Type not supported: " + tc);
		//}
		

		public override void Deserialize(ref MemoryPackReader reader, scoped ref object? value)
		{
			if (!reader.TryReadObjectHeader(out var count))
			{
				value = null;
				return;
			}

			if (count != 2) MemoryPackSerializationException.ThrowInvalidPropertyCount(2, count);

			//var typeName = reader.ReadString();
			//var type = Type.GetType(typeName!);
			//	TypeCode tc = (TypeCode)reader.ReadValue<byte>();
			//ValidateTypeCode(tc);
			var size = reader.ReadVarIntInt32();

			var sr = reader.GetSpanReference(size);


			//var span = CollectionsMarshalEx.CreateSpan(value, length);
			//reader.ReadSpanWithoutReadLengthHeader(size, ref )

			//reader.ReadValue(tc.GetType(), ref value);
		}
	}

	public sealed class UnsafeObjectArrayFormatter2 : MemoryPackFormatter<object?[]>
	{
		public static readonly UnsafeObjectArrayFormatter2 Default = new UnsafeObjectArrayFormatter2();

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
#endif