#if false
using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Buffers;

namespace GoreRemoting.Serialization.MessagePack
{

	internal class MessagePackFormatterConverter : IFormatterConverter
	{
		private readonly MessagePackSerializerOptions options;

		internal MessagePackFormatterConverter(MessagePackSerializerOptions options)
		{
			this.options = options;
		}

		public object Convert(object value, Type type) => ((RawMessagePack)value).Deserialize(type, this.options);

		public object Convert(object value, TypeCode typeCode)
		{
			return typeCode switch
			{
				TypeCode.Object => ((RawMessagePack)value).Deserialize<object>(this.options),
				_ => ExceptionSerializationHelpers.Convert(this, value, typeCode),
			};
		}

		public bool ToBoolean(object value) => ((RawMessagePack)value).Deserialize<bool>(this.options);

		public byte ToByte(object value) => ((RawMessagePack)value).Deserialize<byte>(this.options);

		public char ToChar(object value) => ((RawMessagePack)value).Deserialize<char>(this.options);

		public DateTime ToDateTime(object value) => ((RawMessagePack)value).Deserialize<DateTime>(this.options);

		public decimal ToDecimal(object value) => ((RawMessagePack)value).Deserialize<decimal>(this.options);

		public double ToDouble(object value) => ((RawMessagePack)value).Deserialize<double>(this.options);

		public short ToInt16(object value) => ((RawMessagePack)value).Deserialize<short>(this.options);

		public int ToInt32(object value) => ((RawMessagePack)value).Deserialize<int>(this.options);

		public long ToInt64(object value) => ((RawMessagePack)value).Deserialize<long>(this.options);

		public sbyte ToSByte(object value) => ((RawMessagePack)value).Deserialize<sbyte>(this.options);

		public float ToSingle(object value) => ((RawMessagePack)value).Deserialize<float>(this.options);

		public string ToString(object value) => ((RawMessagePack)value).Deserialize<string>(this.options);

		public ushort ToUInt16(object value) => ((RawMessagePack)value).Deserialize<ushort>(this.options);

		public uint ToUInt32(object value) => ((RawMessagePack)value).Deserialize<uint>(this.options);

		public ulong ToUInt64(object value) => ((RawMessagePack)value).Deserialize<ulong>(this.options);
	}


	internal struct RawMessagePack
	{
		private readonly ReadOnlySequence<byte> rawSequence;

		private RawMessagePack(ReadOnlySequence<byte> raw)
		{
			this.rawSequence = raw;
		}

		/// <summary>
		/// Reads one raw messagepack token.
		/// </summary>
		/// <param name="reader">The reader to use.</param>
		/// <param name="copy"><c>true</c> if the token must outlive the lifetime of the reader's underlying buffer; <c>false</c> otherwise.</param>
		/// <returns>The raw messagepack slice.</returns>
		internal static RawMessagePack ReadRaw(ref MessagePackReader reader)
		{
			return new RawMessagePack(reader.ReadRaw());
		}

		internal void WriteRaw(ref MessagePackWriter writer)
		{
			writer.WriteRaw(this.rawSequence);
		}

		internal object Deserialize(Type type, MessagePackSerializerOptions options)
		{
			return 
				MessagePackSerializer.Deserialize(type, this.rawSequence, options);
		}

		internal T Deserialize<T>(MessagePackSerializerOptions options)
		{
			return	MessagePackSerializer.Deserialize<T>(this.rawSequence, options);
		}
	}


}
#endif