// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using GoreRemoting.Nerdbank.Streams;

namespace GoreRemoting
{
	static class ExceptionConverter
	{
		static readonly JsonSerializerOptions _options = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		const string StackTraceStringKey = "StackTraceString";
		const string InnerExceptionStringKey = "InnerExceptionString";
		const string InnerExceptionKey = "InnerException";
		const string RemoteStackTraceStringKey = "RemoteStackTraceString";
		/// <summary>
		/// The name of the value stored by exceptions that stores watson bucket information.
		/// </summary>
		/// <remarks>
		/// This value should be suppressed when writing or reading exceptions as it is irrelevant to
		/// remote parties and otherwise adds to the size to the payload.
		/// </remarks>
		internal const string WatsonBucketsKey = "WatsonBuckets";

		private const string AssemblyNameKeyName = "AssemblyName";

		internal const string ClassNameKey = "ClassName";

		public static Exception ToException(Dictionary<string, string> dict)
		{
			SerializationInfo info = GetInfo(dict);
			return Deserialize(info);
		}

		private static SerializationInfo GetInfo(Dictionary<string, string> dict)
		{
			SerializationInfo? info = new(typeof(Exception), new JsonFormatterConverter(_options));

			var fullStackTrace = new StringBuilder();

			var remoteStackTraceStringNode = JsonNode.Parse(dict[RemoteStackTraceStringKey]);
			fullStackTrace.Append(remoteStackTraceStringNode?.GetValue<string>());

			if (dict.TryGetValue(InnerExceptionStringKey, out var ies))
			{
				var iesStr = JsonNode.Parse(ies)?.GetValue<string>();
				if (!string.IsNullOrEmpty(iesStr))
				{
					//if (string.IsNullOrEmpty(ie)) // protobuff: null becomes ""
					fullStackTrace.Append(" ---> ");
					fullStackTrace.Append(iesStr);
					fullStackTrace.Append(Environment.NewLine);
					// End of...is only added if inner exception has a stack trace (not just a message)
					if (iesStr.IndexOf("\n   at ") > 0)
					{
						fullStackTrace.Append("   ");
						fullStackTrace.Append("--- End of inner exception stack trace ---");
						fullStackTrace.Append(Environment.NewLine);
					}
				}
			}

			var stackTraceStringNode = JsonNode.Parse(dict[StackTraceStringKey]);

			fullStackTrace.Append(stackTraceStringNode?.GetValue<string>());
			fullStackTrace.Append(Environment.NewLine);
			//SR.Exception_EndStackTraceFromPreviousThrow 
			fullStackTrace.Append("--- End of stack trace from previous location ---");
			fullStackTrace.Append(Environment.NewLine);

			foreach (var kv in dict)
			{
				if (kv.Key == WatsonBucketsKey)
				{
					// ignore, skip
				}
				else if (kv.Key == InnerExceptionKey)
				{
					// write null
					info.AddValue(kv.Key, null);
				}
				else if (kv.Key == StackTraceStringKey)
				{
					// StackTraceStringKey now part of RemoteStackTraceStringKey, so null it here
					info.AddValue(kv.Key, null);
				}
				else if (kv.Key == RemoteStackTraceStringKey)
				{
					info.AddValue(kv.Key, JsonValue.Create(fullStackTrace.ToString()));
				}
				else
				{
					info.AddValue(kv.Key, JsonNode.Parse(kv.Value));
				}
			}

			return info;
		}

		public static Dictionary<string, string> ToDict(Exception value)
		{
			Dictionary<string, string> dict = new();

			SerializationInfo info = new(value.GetType(), new JsonFormatterConverter(_options));

			Serialize(value, info);

			foreach (SerializationEntry element in info)
			{
				if (element.Name == WatsonBucketsKey)
				{
					// ignore, skip
				}
				else if (element.Name == InnerExceptionKey)
				{
					// write null
					var str = JsonSerializer.Serialize<Exception?>(null, _options);
					dict.Add(element.Name, str);
				}
				else
				{
					var str = JsonSerializer.Serialize(element.Value, _options);
					dict.Add(element.Name, str);
				}
			}

			return dict;
		}

		private static readonly Type[] DeserializingConstructorParameterTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

		private static StreamingContext Context => new(StreamingContextStates.Remoting);

		internal static Exception Deserialize(SerializationInfo info)
		{
			if (!TryGetValue(info, ClassNameKey, out string? runtimeTypeName) || runtimeTypeName is null)
			{
				throw new NotSupportedException("ClassName was not found in the serialized data.");
			}

			TryGetValue(info, AssemblyNameKeyName, out string? runtimeAssemblyName);
			Type? runtimeType = LoadType(runtimeTypeName, runtimeAssemblyName);
			if (runtimeType is null)
			{
				// fallback
				return DeserializeRemoteInvocationException(info);
			}

			// Sanity/security check: ensure the runtime type derives from the expected type.
			if (!typeof(Exception).IsAssignableFrom(runtimeType))
			{
				throw new NotSupportedException($"{runtimeTypeName} does not derive from {typeof(Exception).FullName}.");
			}

			ConstructorInfo? ctor = FindDeserializingConstructor(runtimeType);
			if (ctor != null)
				return (Exception)ctor.Invoke(new object[] { info, Context });

			Type? baseType = runtimeType.BaseType;
			while (baseType != null)
			{
				ctor = FindDeserializingConstructor(baseType);
				if (ctor != null)
					break;

				baseType = baseType.BaseType;
			}

			if (ctor == null)
				throw new NotSupportedException("Deserializing constructor (SerializationInfo, StreamingContext) not found in any base type (impossible)");

#if NETSTANDARD2_1_OR_GREATER
			var res = (Exception)RuntimeHelpers.GetUninitializedObject(runtimeType);
#else
			var res = (Exception)FormatterServices.GetUninitializedObject(runtimeType);
#endif
			// TODO: Support a derived exception that crash in its deserializing exception? And/or ignore that invoke fails?
			ctor.Invoke(res, new object[] { info, Context });
			return res;
		}

		private static RemoteInvocationException DeserializeRemoteInvocationException(SerializationInfo info)
		{
			return new RemoteInvocationException(info, Context);
		}


		/// <summary>
		/// Attempts to load a type based on its full name and possibly assembly name.
		/// </summary>
		/// <param name="typeFullName">The <see cref="Type.FullName"/> of the type to be loaded.</param>
		/// <param name="assemblyName">The assemble name that is expected to define the type, if available. This should be parseable by <see cref="AssemblyName(string)"/>.</param>
		/// <returns>The loaded <see cref="Type"/>, if one could be found; otherwise <see langword="null" />.</returns>
		/// <remarks>
		/// <para>
		/// This method is used to load types that are strongly referenced by incoming messages during serialization.
		/// It is important to not load types that may pose a security threat based on the type and the trust level of the remote party.
		/// </para>
		/// <para>
		/// The default implementation of this method loads any type named if it can be found based on its assembly name (if provided) or based on any assembly already loaded in the AppDomain otherwise.
		/// </para>
		/// <para>Implementations should avoid throwing <see cref="FileLoadException"/>, <see cref="TypeLoadException"/> or other exceptions, preferring to return <see langword="null" /> instead.</para>
		/// </remarks>
		static Type? LoadType(string typeFullName, string? assemblyName)
		{
			Requires.NotNull(typeFullName, nameof(typeFullName));

			Assembly? typeDeclaringAssembly = null;
			if (assemblyName is object)
			{
				try
				{
					typeDeclaringAssembly = Assembly.Load(assemblyName);
				}
				catch (Exception ex) when (ex is FileNotFoundException or FileLoadException)
				{
					// Try removing the version from the AssemblyName and try again, in case the message came from a newer version.
					var an = new AssemblyName(assemblyName);
					if (an.Version is object)
					{
						an.Version = null;
						try
						{
							typeDeclaringAssembly = Assembly.Load(an.FullName);
						}
						catch (Exception exRedux) when (exRedux is FileNotFoundException or FileLoadException)
						{
							// If we fail again, we'll just try to load the exception type from the AppDomain without an assembly's context.
						}
					}
				}
			}

			Type? runtimeType = typeDeclaringAssembly is object ? typeDeclaringAssembly.GetType(typeFullName) : Type.GetType(typeFullName);
			return runtimeType;
		}

		internal static void Serialize(Exception exception, SerializationInfo info)
		{
			exception.GetObjectData(info, Context);
			info.AddValue(AssemblyNameKeyName, exception.GetType().Assembly.FullName);
			info.AddValue(InnerExceptionStringKey, exception.InnerException?.ToString());
		}

		private static ConstructorInfo? FindDeserializingConstructor(Type runtimeType)
			=> runtimeType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, DeserializingConstructorParameterTypes, null);

		private static bool TryGetValue(SerializationInfo info, string key, out string? value)
		{
			try
			{
				value = info.GetString(key);
				return true;
			}
			catch (SerializationException)
			{
				value = null;
				return false;
			}
		}

		internal static Exception ReadRemoteInvocationException(Dictionary<string, string> dict)
		{
			SerializationInfo info = GetInfo(dict);

			return DeserializeRemoteInvocationException(
				info
				);
		}
	
	}

	class JsonFormatterConverter : IFormatterConverter
	{
		private readonly JsonSerializerOptions serializerOptions;

		internal JsonFormatterConverter(JsonSerializerOptions serializerOptions)
		{
			this.serializerOptions = serializerOptions;
		}

		public object? Convert(object value, Type type)
		{
			var jsonValue = (JsonNode?)value;
			if (jsonValue is null)
			{
				return null;
			}

			if (type == typeof(System.Collections.IDictionary))
			{
				return DeserializePrimitive(jsonValue);
			}

			return jsonValue.Deserialize(type, this.serializerOptions)!;
		}

		public object Convert(object value, TypeCode typeCode)
		{
			return typeCode switch
			{
				TypeCode.Object => ((JsonNode)value).Deserialize(typeof(object), this.serializerOptions)!,
				_ => Convert(this, value, typeCode),
			};
		}

		internal static object Convert(IFormatterConverter formatterConverter, object value, TypeCode typeCode)
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

		public bool ToBoolean(object value) => GetValue<bool>(value);
		public byte ToByte(object value) => GetValue<byte>(value);
		public char ToChar(object value) => GetValue<char>(value);
		public DateTime ToDateTime(object value) => GetValue<DateTime>(value);
		public decimal ToDecimal(object value) => GetValue<decimal>(value);
		public double ToDouble(object value) => GetValue<double>(value);
		public short ToInt16(object value) => GetValue<short>(value);
		public int ToInt32(object value) => GetValue<int>(value);
		public long ToInt64(object value) => GetValue<long>(value);
		public sbyte ToSByte(object value) => GetValue<sbyte>(value);
		public float ToSingle(object value) => GetValue<float>(value);
		public string? ToString(object value) => GetValue<string>(value);
		public ushort ToUInt16(object value) => GetValue<ushort>(value);
		public uint ToUInt32(object value) => GetValue<uint>(value);
		public ulong ToUInt64(object value) => GetValue<ulong>(value);

		private static T GetValue<T>(object value)
		{
			return ((JsonNode)value).GetValue<T>();
		}

		private static object? DeserializePrimitive(JsonNode? node)
		{
			return node switch
			{
				JsonObject o => DeserializeObjectAsDictionary(o),
				JsonValue v => DeserializePrimitiveOrToString(v.GetValue<JsonElement>()),
				JsonArray a => a.Select(DeserializePrimitive).ToArray(),
				null => null,
				_ => throw new NotSupportedException("Unrecognized node type: " + node.GetType().Name),
			};
		}

		private static Dictionary<string, object?> DeserializeObjectAsDictionary(JsonNode jsonNode)
		{
			Dictionary<string, object?> dictionary = new();
			foreach (KeyValuePair<string, JsonNode?> property in jsonNode.AsObject())
			{
				dictionary.Add(property.Key, DeserializePrimitive(property.Value));
			}

			return dictionary;
		}

		private static object? DeserializePrimitiveOrToString(JsonElement element)
		{
			return element.ValueKind switch
			{
				JsonValueKind.String => element.GetString(),
				JsonValueKind.Number => GetNumber(element),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				_ => element.ToString() // throw new NotSupportedException(),
			};
		}

		private static object GetNumber(JsonElement element)
		{
			if (element.TryGetInt32(out int intValue))
				return intValue;
			if (element.TryGetInt64(out long longValue))
				return longValue;
			if (element.TryGetDouble(out double doubleValue))
				return doubleValue;

			return element.ToString();
		}
	}

	//internal static class ExceptionSerializationHelpers
	//{
	
	//}

}
