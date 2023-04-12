using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Buffers;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{





#if false

	internal class MemoryPackFormatterConverter : IFormatterConverter
	{
		private readonly MemoryPackSerializerOptions options;

		internal MemoryPackFormatterConverter(MemoryPackSerializerOptions options)
		{
			this.options = options;
		}

		public object Convert(object value, Type type) => ((RawMemoryPack)value).Deserialize(type, this.options);

		public object Convert(object value, TypeCode typeCode)
		{
			return typeCode switch
			{
				TypeCode.Object => ((RawMemoryPack)value).Deserialize<object>(this.options),
				_ => ExceptionSerializationHelpers.Convert(this, value, typeCode),
			};
		}

		public bool ToBoolean(object value) => ((RawMemoryPack)value).Deserialize<bool>(this.options);

		public byte ToByte(object value) => ((RawMemoryPack)value).Deserialize<byte>(this.options);

		public char ToChar(object value) => ((RawMemoryPack)value).Deserialize<char>(this.options);

		public DateTime ToDateTime(object value) => ((RawMemoryPack)value).Deserialize<DateTime>(this.options);

		public decimal ToDecimal(object value) => ((RawMemoryPack)value).Deserialize<decimal>(this.options);

		public double ToDouble(object value) => ((RawMemoryPack)value).Deserialize<double>(this.options);

		public short ToInt16(object value) => ((RawMemoryPack)value).Deserialize<short>(this.options);

		public int ToInt32(object value) => ((RawMemoryPack)value).Deserialize<int>(this.options);

		public long ToInt64(object value) => ((RawMemoryPack)value).Deserialize<long>(this.options);

		public sbyte ToSByte(object value) => ((RawMemoryPack)value).Deserialize<sbyte>(this.options);

		public float ToSingle(object value) => ((RawMemoryPack)value).Deserialize<float>(this.options);

		public string ToString(object value) => ((RawMemoryPack)value).Deserialize<string>(this.options);

		public ushort ToUInt16(object value) => ((RawMemoryPack)value).Deserialize<ushort>(this.options);

		public uint ToUInt32(object value) => ((RawMemoryPack)value).Deserialize<uint>(this.options);

		public ulong ToUInt64(object value) => ((RawMemoryPack)value).Deserialize<ulong>(this.options);
	}
#endif

//	internal struct RawMemoryPack
//	{
//		private readonly ReadOnlySequence<byte> rawSequence;

//		private readonly ReadOnlyMemory<byte> rawMemory;

//		private RawMemoryPack(ReadOnlySequence<byte> raw)
//		{
//			this.rawSequence = raw;
//			this.rawMemory = default;
//		}

//		private RawMemoryPack(ReadOnlyMemory<byte> raw)
//		{
//			this.rawSequence = default;
//			this.rawMemory = raw;
//		}

//		internal bool IsDefault => this.rawMemory.IsEmpty && this.rawSequence.IsEmpty;

//		/// <summary>
//		/// Reads one raw messagepack token.
//		/// </summary>
//		/// <param name="reader">The reader to use.</param>
//		/// <param name="copy"><c>true</c> if the token must outlive the lifetime of the reader's underlying buffer; <c>false</c> otherwise.</param>
//		/// <returns>The raw messagepack slice.</returns>
//		internal static RawMemoryPack ReadRaw(ref MemoryPackReader reader, bool copy)
//		{
////			SequencePosition initialPosition = reader.Consumed;// ..Position;
//			reader.ReadUnmanaged
//	//		ReadOnlySequence<byte> slice = reader..Sequence.Slice(initialPosition, reader.Position);
//		//	return copy ? new RawMemoryPack(slice.ToArray()) : new RawMemoryPack(slice);
//		}

//		internal void WriteRaw<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer)
//		{
//			if (this.rawSequence.IsEmpty)
//			{
//				writer.WriteRaw(this.rawMemory.Span);
//			}
//			else
//			{
//				writer.WriteRaw(this.rawSequence);
//			}
//		}

//		internal object Deserialize(Type type, MessagePackSerializerOptions options)
//		{
//			return this.rawSequence.IsEmpty
//				? MessagePackSerializer.Deserialize(type, this.rawMemory, options)
//				: MessagePackSerializer.Deserialize(type, this.rawSequence, options);
//		}

//		internal T Deserialize<T>(MessagePackSerializerOptions options)
//		{
//			return this.rawSequence.IsEmpty
//				? MessagePackSerializer.Deserialize<T>(this.rawMemory, options)
//				: MessagePackSerializer.Deserialize<T>(this.rawSequence, options);
//		}
//	}


	//private class ResolverWrapper : IFormatterResolver
	//{
	//	private readonly IFormatterResolver inner;

	//	internal ResolverWrapper(IFormatterResolver inner, MessagePackFormatter formatter)
	//	{
	//		this.inner = inner;
	//		this.Formatter = formatter;
	//	}

	//	internal MessagePackFormatter Formatter { get; }

	//	public IMessagePackFormatter<T> GetFormatter<T>() => this.inner.GetFormatter<T>();
	//}

}
