#if false
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GoreRemoting
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;

	public static class ExceptionSerializationHelpers
	{
		private static readonly Type[] DeserializingConstructorParameterTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

		private static StreamingContext Context => new StreamingContext(StreamingContextStates.Remoting);

		public static Exception DeserializingConstructor(Type type, SerializationInfo info)
		{


			//Type? runtimeType = Type.GetType(typeName);// runtimeTypeName);
			//if (runtimeType is null)
			//{
			//	if (traceSource?.Switch.ShouldTrace(TraceEventType.Warning) ?? false)
			//	{
			//		traceSource.TraceEvent(TraceEventType.Warning, 1,//(int)JsonRpc.TraceEvents.ExceptionTypeNotFound, 
			//			"{0} type could not be loaded. Falling back to System.Exception.", typeName);// runtimeTypeName);
			//	}

			//	// fallback to deserializing the base Exception type.
			//	runtimeType = typeof(RemoteInvocationException);
			//	//return null;
			//	//throw new Exception("Type not found");
			//}

			// Sanity/security check: ensure the runtime type derives from the expected type.
			//if (!typeof(T).IsAssignableFrom(runtimeType))
			//{
			//	throw new NotSupportedException($"{typeName} does not derive from {typeof(T).FullName}.");
			//}

			//			EnsureSerializableAttribute(runtimeType);

			ConstructorInfo ctor = FindDeserializingConstructor(type);
			if (ctor is null)
			{
				throw new Exception("deserializing constructor not found");
				//throw new NotSupportedException($"{runtimeType.FullName} does not declare a deserializing constructor with signature ({string.Join(", ", DeserializingConstructorParameterTypes.Select(t => t.FullName))}).");
			}

			var res = (Exception)ctor.Invoke(new object?[] { info, Context });

	

			return res;
		}

		public static SerializationInfo GetObjectData(Exception ex)//,  info)
		{
			SerializationInfo info = new SerializationInfo(ex.GetType(), new DummyConverterFormatter());

			//Type exceptionType = exception.GetType();
			//			EnsureSerializableAttribute(exceptionType);
			ex.GetObjectData(info, Context);

			return info;
		}

		public static object Convert(IFormatterConverter formatterConverter, object value, TypeCode typeCode)
		{
			return typeCode switch
			{
				TypeCode.Boolean => formatterConverter.ToBoolean(value),
				TypeCode.Byte => formatterConverter.ToBoolean(value),
				TypeCode.Char => formatterConverter.ToChar(value),
				TypeCode.DateTime => formatterConverter.ToDateTime(value),
				TypeCode.Decimal => formatterConverter.ToDecimal(value),
				TypeCode.Double => formatterConverter.ToDouble(value),
				TypeCode.Int16 => formatterConverter.ToInt16(value),
				TypeCode.Int32 => formatterConverter.ToInt32(value),
				TypeCode.Int64 => formatterConverter.ToInt64(value),
				TypeCode.SByte => formatterConverter.ToSByte(value),
				TypeCode.Single => formatterConverter.ToSingle(value),
				TypeCode.String => formatterConverter.ToString(value),
				TypeCode.UInt16 => formatterConverter.ToUInt16(value),
				TypeCode.UInt32 => formatterConverter.ToUInt32(value),
				TypeCode.UInt64 => formatterConverter.ToUInt64(value),
				_ => throw new NotSupportedException("Unsupported type code: " + typeCode),
			};
		}

		//private static void EnsureSerializableAttribute(Type runtimeType)
		//{
		//	if (runtimeType.GetCustomAttribute<SerializableAttribute>() is null)
		//	{
		//		throw new NotSupportedException($"{runtimeType.FullName} is not marked with the {typeof(SerializableAttribute).FullName}.");
		//	}
		//}

		private static ConstructorInfo? FindDeserializingConstructor(Type runtimeType) => runtimeType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, DeserializingConstructorParameterTypes, null);
	}

	public class DummyConverterFormatter : IFormatterConverter
	{


		public object Convert(object value, Type type) => throw new NotImplementedException();

		public object Convert(object value, TypeCode typeCode) => throw new NotImplementedException();


		public bool ToBoolean(object value) => throw new NotImplementedException();

		public byte ToByte(object value) => throw new NotImplementedException();

		public char ToChar(object value) => throw new NotImplementedException();

		public DateTime ToDateTime(object value) => throw new NotImplementedException();

		public decimal ToDecimal(object value) => throw new NotImplementedException();

		public double ToDouble(object value) => throw new NotImplementedException();

		public short ToInt16(object value) => throw new NotImplementedException();

		public int ToInt32(object value) => throw new NotImplementedException();

		public long ToInt64(object value) => throw new NotImplementedException();

		public sbyte ToSByte(object value) => throw new NotImplementedException();

		public float ToSingle(object value) => throw new NotImplementedException();

		public string ToString(object value) => throw new NotImplementedException();

		public ushort ToUInt16(object value) => throw new NotImplementedException();

		public uint ToUInt32(object value) => throw new NotImplementedException();

		public ulong ToUInt64(object value) => throw new NotImplementedException();
	}

}
#endif