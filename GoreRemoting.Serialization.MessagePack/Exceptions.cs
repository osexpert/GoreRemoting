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
	/// <summary>
	/// Manages serialization of any <see cref="Exception"/>-derived type that follows standard <see cref="SerializableAttribute"/> rules.
	/// </summary>
	/// <remarks>
	/// A serializable class will:
	/// 1. Derive from <see cref="Exception"/>
	/// 2. Be attributed with <see cref="SerializableAttribute"/>
	/// 3. Declare a constructor with a signature of (<see cref="SerializationInfo"/>, <see cref="StreamingContext"/>).
	/// </remarks>
	internal class MessagePackExceptionResolver : IFormatterResolver
	{
		internal static readonly MessagePackExceptionResolver Instance = new MessagePackExceptionResolver();

		private MessagePackExceptionResolver()
		{
		}

		public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

		private static class Cache<T>
		{
			internal static readonly IMessagePackFormatter<T>? Formatter;

			static Cache()
			{
				if (typeof(Exception).IsAssignableFrom(typeof(T)) && typeof(T).GetCustomAttribute<SerializableAttribute>() is object)
				{
					Formatter = (IMessagePackFormatter<T>)Activator.CreateInstance(typeof(ExceptionFormatter<>).MakeGenericType(typeof(T)));
				}
			}
		}

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
		private class ExceptionFormatter<T> : IMessagePackFormatter<T>
			where T : Exception
		{

#if NETSTANDARD2_1_OR_GREATER
			[return: MaybeNull]
#endif
			public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
			{
				if (reader.TryReadNil())
				{
					return null;
				}

				var info = new SerializationInfo(typeof(T), new MessagePackFormatterConverter(options));
				int memberCount = reader.ReadMapHeader();
				for (int i = 0; i < memberCount; i++)
				{
					string name = reader.ReadString();
					object value = RawMessagePack.ReadRaw(ref reader, false);
					info.AddValue(name, value);
				}

				//var resolverWrapper = options.Resolver as ResolverWrapper;
				//					Report.If(resolverWrapper is null, "Unexpected resolver type.");
				return ExceptionSerializationHelpers.Deserialize<T>(info, null);// resolverWrapper?.Formatter.rpc?.TraceSource);
			}

			public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
			{
				if (value is null)
				{
					writer.WriteNil();
					return;
				}

				var info = new SerializationInfo(typeof(T), new MessagePackFormatterConverter(options));
				ExceptionSerializationHelpers.Serialize(value, info);
				writer.WriteMapHeader(info.MemberCount);
				foreach (SerializationEntry element in info)
				{
					writer.Write(element.Name);
					MessagePackSerializer.Serialize(element.ObjectType, ref writer, element.Value, options);
				}
			}
		}
#pragma warning restore
	}








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

		private readonly ReadOnlyMemory<byte> rawMemory;

		private RawMessagePack(ReadOnlySequence<byte> raw)
		{
			this.rawSequence = raw;
			this.rawMemory = default;
		}

		private RawMessagePack(ReadOnlyMemory<byte> raw)
		{
			this.rawSequence = default;
			this.rawMemory = raw;
		}

		internal bool IsDefault => this.rawMemory.IsEmpty && this.rawSequence.IsEmpty;

		/// <summary>
		/// Reads one raw messagepack token.
		/// </summary>
		/// <param name="reader">The reader to use.</param>
		/// <param name="copy"><c>true</c> if the token must outlive the lifetime of the reader's underlying buffer; <c>false</c> otherwise.</param>
		/// <returns>The raw messagepack slice.</returns>
		internal static RawMessagePack ReadRaw(ref MessagePackReader reader, bool copy)
		{
			SequencePosition initialPosition = reader.Position;
			reader.Skip();
			ReadOnlySequence<byte> slice = reader.Sequence.Slice(initialPosition, reader.Position);
			return copy ? new RawMessagePack(slice.ToArray()) : new RawMessagePack(slice);
		}

		internal void WriteRaw(ref MessagePackWriter writer)
		{
			if (this.rawSequence.IsEmpty)
			{
				writer.WriteRaw(this.rawMemory.Span);
			}
			else
			{
				writer.WriteRaw(this.rawSequence);
			}
		}

		internal object Deserialize(Type type, MessagePackSerializerOptions options)
		{
			return this.rawSequence.IsEmpty
				? MessagePackSerializer.Deserialize(type, this.rawMemory, options)
				: MessagePackSerializer.Deserialize(type, this.rawSequence, options);
		}

		internal T Deserialize<T>(MessagePackSerializerOptions options)
		{
			return this.rawSequence.IsEmpty
				? MessagePackSerializer.Deserialize<T>(this.rawMemory, options)
				: MessagePackSerializer.Deserialize<T>(this.rawSequence, options);
		}
	}


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
