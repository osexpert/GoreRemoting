#if false
using MemoryPack;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections;
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
	public sealed class SerInfoFormatterAttribute : MemoryPackCustomFormatterAttribute<SerInfoFormatter, object>
	{
		public override SerInfoFormatter GetFormatter()
		{
			return SerInfoFormatter.Default;
		}
	}

	public sealed class SerInfoArrayFormatterAttribute : MemoryPackCustomFormatterAttribute<SerInfoArrayFormatter, object?[]>
	{
		public override SerInfoArrayFormatter GetFormatter()
		{
			return SerInfoArrayFormatter.Default;
		}
	}

	public sealed class SerInfoFormatter : MemoryPackFormatter<object>
	{
		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public static readonly SerInfoFormatter Default = new SerInfoFormatter();

		public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object? value)
		{


			if (value == null)
			{
				writer.WriteNullObjectHeader();
				return;
			}

//			int? len = null;
	//		byte[] data = null;
			try
			{
				using (var s = (RecyclableMemoryStream)_manager.GetStream())
				{
					var ss = s; // silence warning
								// WHY is ref used here???
					var w = new MemoryPackWriter<RecyclableMemoryStream>(ref ss, writer.OptionalState);
					w.WriteValue(value.GetType(), value);
					w.Flush();

					// or use length?
					//writer.WriteVarInt((int)s.Length);

					var len = (int)s.Length;

					//var data = s.GetBuffer().AsMemory(0, len);
					var data = s.GetBuffer().AsSpan(0, len);

					writer.WriteObjectHeader(1);
					writer.WriteSpan(data);//.Span);

					//				var sr = writer.GetSpanReference((int)s.Length);
					//Unsafe.WriteUnaligned(ref sr, s.ToArray());
					//			var from = s.GetBuffer().AsSpan();
					//				Unsafe.CopyBlock(ref sr, ref MemoryMarshal.GetReference(from), (uint)s.Length);
					//		writer.Advance((int)s.Length);
				}

			}
			catch (MemoryPackSerializationException)
			{
				writer.WriteNullObjectHeader();
				return;
			}


			//writer.WriteObjectHeader(2);

			//writer.WriteArray<byte>()

			//writer.WriteVarInt(len.Value);

			//writer.WriteValue(value.GetType(), value);

			//			var type = value.GetType();
			//			//string typeName = TypeShortener.GetShortType(type);

			//			writer.WriteObjectHeader(2);
			//			//writer.WriteString(typeName);

			//			var srSize = writer.GetSpanReference(4);
			//			writer.WriteValue<int>(-1); // dummy

			//			//writer.Advance(4);

			//			var wc = writer.WrittenCount;
			//			try
			//			{

			//				writer.WriteValue(type, value);
			//			}
			//			catch (Exception e)
			//			{
			//			}
			//			////writer.Flush();
			//			var len = writer.WrittenCount - wc;



			//			Unsafe.WriteUnaligned<int>(ref srSize, len);

			//			//writer.Advance(- (len + 4));
			//			//writer.WriteValue<Int32>(len);
			//			//writer.Advance((len + 4));

			//			// make a fake write to get the length
			//			using (var s = (RecyclableMemoryStream)_manager.GetStream())
			//			{
			//				var ss = s; // silence warning
			//							// WHY is ref used here???
			//				var w = new MemoryPackWriter<RecyclableMemoryStream>(ref ss, writer.OptionalState);
			//				w.WriteValue(type, value);
			//				w.Flush();

			//				// or use length?
			//				writer.WriteVarInt((int)s.Length);
			////				var sr = writer.GetSpanReference((int)s.Length);
			//				//Unsafe.WriteUnaligned(ref sr, s.ToArray());
			//	//			var from = s.GetBuffer().AsSpan();
			////				Unsafe.CopyBlock(ref sr, ref MemoryMarshal.GetReference(from), (uint)s.Length);
			//		//		writer.Advance((int)s.Length);
			//			}


		}

		public override void Deserialize(ref MemoryPackReader reader, scoped ref object? value)
		{
			if (!reader.TryReadObjectHeader(out var count))
			{
				value = null;
				return;
			}

			if (count != 1) MemoryPackSerializationException.ThrowInvalidPropertyCount(1, count);

			//var len = reader.ReadValue<int>();

			//new ReadOnlySequence<byte>()


			//var len = reader.ReadVarIntInt32();

			//var typeName = reader.ReadString();
			//var type = Type.GetType(typeName!);
			//reader.ReadValue(type!, ref value);


			//var reff = MemoryMarshal.CreateReadOnlySpan(ref reader.GetSpanReference(len), len);


			//reff.
			//object ob = reff;
			//reader.ReadArray()
			//MemoryMarshal.AsBytes(reader.ReadSpan<>)


			var bytes = reader.ReadArray<byte>();

			var f = new RawMemoryPack() { ByteSeq = bytes };
//			reader.Advance(len);

			value = f;// ref reff;
		}
	}

	public class RawMemoryPack
	{
		//public int Len;
		public ReadOnlyMemory<byte> ByteSeq { get; set; }

		internal object Deserialize(Type type, MemoryPackSerializerOptions options)
		{
			return MemoryPackSerializer.Deserialize(type, ByteSeq.Span, options);
		}

		internal T Deserialize<T>(MemoryPackSerializerOptions options)
		{
			return MemoryPackSerializer.Deserialize<T>(ByteSeq.Span, options);
		}
	}

	public sealed class SerInfoArrayFormatter : MemoryPackFormatter<object?[]>
	{
		public static readonly SerInfoArrayFormatter Default = new SerInfoArrayFormatter();

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
				SerInfoFormatter.Default.Serialize(ref writer, ref v);
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
				SerInfoFormatter.Default.Deserialize(ref reader, ref v);
				value[i] = v;
			}
		}
	}
}
#endif