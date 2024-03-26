using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using Nerdbank.Streams;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Xml.Linq;
using System.Collections.Specialized;
using System.Numerics;

namespace GoreRemoting
{
	class ExceptionConverter// : JsonConverter<Exception>
	{
		/// <summary>
		/// Tracks recursion count while serializing or deserializing an exception.
		/// </summary>
		private static ThreadLocal<int> exceptionRecursionCounter = new();

		//private readonly SystemTextJsonFormatter formatter;
		JsonSerializerOptions _options;

		internal ExceptionConverter(JsonSerializerOptions opt)//SystemTextJsonFormatter formatter)
		{
			_options = opt;
			//this.formatter = formatter;
		}

		//public override bool CanConvert(Type typeToConvert) => typeof(Exception).IsAssignableFrom(typeToConvert);

		//public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		public Exception Read(Dictionary<string, string> dict)
		{
			//Assumes.NotNull(this.formatter.JsonRpc);

			exceptionRecursionCounter.Value++;
			try
			{
				//if (reader.TokenType != JsonTokenType.StartObject)
				//{
				//	throw new InvalidOperationException("Expected a StartObject token.");
				//}

				//if (exceptionRecursionCounter.Value > this.formatter.JsonRpc.ExceptionOptions.RecursionLimit)
				//{
				//	// Exception recursion has gone too deep. Skip this value and return null as if there were no inner exception.
				//	// Note that in skipping, the parser may use recursion internally and may still throw if its own limits are exceeded.
				//	reader.Skip();
				//	return null;
				//}

				SerializationInfo? info = new SerializationInfo(typeof(Exception), new JsonFormatterConverter(_options));// this.formatter.massagedUserDataSerializerOptions));

				//				JsonNode? jsonNode = JsonNode.Parse(ref reader) ?? throw new JsonException("Unexpected null");
				//JsonNode? jsonNode = JsonNode.Parse(ref reader) ?? throw new JsonException("Unexpected null");



				//foreach (KeyValuePair<string, JsonNode?> property in jsonNode.AsObject())
				//{
				//	info.AddSafeValue(property.Key, property.Value);
				//}

				foreach (var kv in dict)
				{
					if (kv.Key == "StackTraceString")
					{
						// bug(?) in dotnet, does not append this text in case context is CrossAppDomain. Do it manually
						var str = JsonNode.Parse(kv.Value)?.GetValue<string>() + Environment.NewLine +
							//SR.Exception_EndStackTraceFromPreviousThrow 
							"--- End of stack trace from previous location ---"
							+ Environment.NewLine;

						if (dict.TryGetValue("InnerExceptionString", out var ies))
						{
							var iesStr = JsonNode.Parse(ies)?.GetValue<string>();
							if (!string.IsNullOrEmpty(iesStr))
							{
								//if (string.IsNullOrEmpty(ie)) // protobuff: null becomes ""
								//							  //if (ie == null)
								//	return StackTrace ?? string.Empty;
								//else
								var newstr = " ---> " + iesStr + Environment.NewLine + "   " + "--- End of inner exception stack trace ---" + Environment.NewLine + str;
								str = newstr;
							}
						}

						info.AddSafeValue(kv.Key, JsonValue.Create(str));
					}
					else
					{
						info.AddSafeValue(kv.Key, JsonNode.Parse(kv.Value));
					}
				}

				return ExceptionSerializationHelpers.Deserialize(
					//this.formatter.JsonRpc, 
					info,
					null
					//this.formatter.JsonRpc?.TraceSource
					);
			}
			finally
			{
				exceptionRecursionCounter.Value--;
			}
		}

		//public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
		public Dictionary<string, string> Write(Exception value)
		{
			Dictionary<string, string> res = new Dictionary<string, string>();
			// We have to guard our own recursion because the serializer has no visibility into inner exceptions.
			// Each exception in the russian doll is a new serialization job from its perspective.
			exceptionRecursionCounter.Value++;
			try
			{
				//if (exceptionRecursionCounter.Value > this.formatter.JsonRpc?.ExceptionOptions.RecursionLimit)
				//{
				//	// Exception recursion has gone too deep. Skip this value and write null as if there were no inner exception.
				//	writer.WriteNullValue();
				//	return;
				//}

				SerializationInfo info = new SerializationInfo(value.GetType(), new JsonFormatterConverter(_options));// this.formatter.massagedUserDataSerializerOptions));
				
				ExceptionSerializationHelpers.Serialize(value, info);
//				writer.WriteStartObject();

				foreach (SerializationEntry element in info.GetSafeMembers())
				{
					//writer.WritePropertyName(element.Name);
					// string string? 
					//JsonSerializer.Serialize(writer, element.Value, options);

					if (element.Name == "InnerException")
					{
						var str = JsonSerializer.Serialize<Exception?>(null, _options);
						res.Add(element.Name, str);
					}
					else
					{
						var str = JsonSerializer.Serialize(element.Value, _options);
						res.Add(element.Name, str);
					}
				}

				return res;
//				writer.WriteEndObject();
			}
			//catch (Exception ex)
			//{
			//	throw new JsonException(ex.Message, ex);
			//}
			finally
			{
				exceptionRecursionCounter.Value--;
			}
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
				_ => ExceptionSerializationHelpers.Convert(this, value, typeCode),
			};
		}

		public bool ToBoolean(object value) => ((JsonNode)value).GetValue<bool>();
		public byte ToByte(object value) => ((JsonNode)value).GetValue<byte>();
		public char ToChar(object value) => ((JsonNode)value).GetValue<char>();
		public DateTime ToDateTime(object value) => ((JsonNode)value).GetValue<DateTime>();
		public decimal ToDecimal(object value) => ((JsonNode)value).GetValue<decimal>();
		public double ToDouble(object value) => ((JsonNode)value).GetValue<double>();
		public short ToInt16(object value) => ((JsonNode)value).GetValue<short>();
		public int ToInt32(object value) => ((JsonNode)value).GetValue<int>();
		public long ToInt64(object value) => ((JsonNode)value).GetValue<long>();
		public sbyte ToSByte(object value) => ((JsonNode)value).GetValue<sbyte>();
		public float ToSingle(object value) => ((JsonNode)value).GetValue<float>();
		public string? ToString(object value) => ((JsonNode)value).GetValue<string>();
		public ushort ToUInt16(object value) => ((JsonNode)value).GetValue<ushort>();
		public uint ToUInt32(object value) => ((JsonNode)value).GetValue<uint>();
		public ulong ToUInt64(object value) => ((JsonNode)value).GetValue<ulong>();
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
				JsonValueKind.Number => element.TryGetInt32(out int intValue) ? intValue : 
					(element.TryGetInt64(out var int64Val) ? int64Val : element.ToString()),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				_ => element.ToString() // throw new NotSupportedException(),
			};
		}
	}

	internal static class ExceptionSerializationHelpers
	{
		/// <summary>
		/// The name of the value stored by exceptions that stores watson bucket information.
		/// </summary>
		/// <remarks>
		/// This value should be suppressed when writing or reading exceptions as it is irrelevant to
		/// remote parties and otherwise adds to the size to the payload.
		/// </remarks>
		internal const string WatsonBucketsKey = "WatsonBuckets";

		private const string AssemblyNameKeyName = "AssemblyName";

		private static readonly Type[] DeserializingConstructorParameterTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

#if false
		///             // If we are constructing a new exception after a cross-appdomain call...
            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                // ...this new exception may get thrown.  It is logically a re-throw, but 
                //  physically a brand-new exception.  Since the stack trace is cleared 
                //  on a new exception, the "_remoteStackTraceString" is provided to 
                //  effectively import a stack trace from a "remote" exception.  So,
                //  move the _stackTraceString into the _remoteStackTraceString.  Note
                //  that if there is an existing _remoteStackTraceString, it will be 
                //  preserved at the head of the new string, so everything works as 
                //  expected.
                // Even if this exception is NOT thrown, things will still work as expected
                //  because the StackTrace property returns the concatenation of the
                //  _remoteStackTraceString and the _stackTraceString.
                _remoteStackTraceString = _remoteStackTraceString + _stackTraceString;
                _stackTraceString = null;
            }
#endif
		private static StreamingContext Context => new StreamingContext(StreamingContextStates.CrossAppDomain);

		internal static Exception Deserialize(SerializationInfo info, TraceSource? traceSource)
		{
			if (!TryGetValue(info, "ClassName", out string? runtimeTypeName) || runtimeTypeName is null)
			{
				throw new NotSupportedException("ClassName was not found in the serialized data.");
			}

			TryGetValue(info, AssemblyNameKeyName, out string? runtimeAssemblyName);
			Type? runtimeType = LoadType(runtimeTypeName, runtimeAssemblyName);
			if (runtimeType is null)
			{
				// fallback to deserializing the base Exception type.
				// TODO: use RemopteInvocationException?
				runtimeType = typeof(Exception);
			}

			// Sanity/security check: ensure the runtime type derives from the expected type.
			if (!typeof(Exception).IsAssignableFrom(runtimeType))
			{
				throw new NotSupportedException($"{runtimeTypeName} does not derive from {typeof(Exception).FullName}.");
			}


			// Find the nearest exception type that implements the deserializing constructor and is deserializable.
			ConstructorInfo? ctor = null;

			Type? runtimeTypeDoWhile = runtimeType;

			do
			{
				ctor = FindDeserializingConstructor(runtimeTypeDoWhile);
				if (ctor != null)
					break;

				runtimeTypeDoWhile = runtimeTypeDoWhile.BaseType;

			} while (runtimeTypeDoWhile != null);

			if (ctor == null)
				throw new Exception("ctor(SerializationInfo, StreamingContext) not found in type or any base type (impossible)");

			Exception? res = null;

			if (runtimeTypeDoWhile == runtimeType)
			{
				res = (Exception)ctor.Invoke(new object[] { info, Context });
			}
			else
			{
#if NETSTANDARD2_1_OR_GREATER
				res = (Exception)RuntimeHelpers.GetUninitializedObject(runtimeType);
#else
				res = (Exception)FormatterServices.GetUninitializedObject(runtimeType);
#endif
				ctor.Invoke(res, new object[] { info, Context });
			}

		//	SetData(res, info);

			return res;

		}

//		private static void SetData(Exception e, SerializationInfo info)
//		{
//			SetData(e, "_message", info.GetString("Message")); // Do not rename (binary serialization)

//			try
//			{
//				var data = info.GetValue("Data", typeof(IDictionary));

//				//new ListDictionary

//				SetData(e, "_data", data);
//			}
//			catch { }
			

////			SetData("_data", (IDictionary?)(info.GetValueNoThrow("Data", typeof(IDictionary))); // Do not rename (binary serialization)

////			SetData(e, "_innerException", () => null);// = (Exception?)(info.GetValue("InnerException", typeof(Exception))); // Do not rename (binary serialization)
//			SetData(e, "_helpURL", info.GetString("HelpURL")); // Do not rename (binary serialization)

//			SetData(e, "_HResult", info.GetInt32("HResult")); // Do not rename (binary serialization)
//			SetData(e, "_source", info.GetString("Source")); // Do not rename (binary serialization)

//			//SetData(e, "_stackTraceString", () => info.GetString("StackTraceString")); // Do not rename (binary serialization)
//			//SetData(e, "_remoteStackTraceString", () => info.GetString("RemoteStackTraceString")); // Do not rename (binary serialization)

//			var st = info.GetString("StackTraceString");
//			var rst = info.GetString("RemoteStackTraceString");

//#if false  // If we are constructing a new exception after a cross-appdomain call...
//            if (context.State == StreamingContextStates.CrossAppDomain)
//            {
//                // ...this new exception may get thrown.  It is logically a re-throw, but 
//                //  physically a brand-new exception.  Since the stack trace is cleared 
//                //  on a new exception, the "_remoteStackTraceString" is provided to 
//                //  effectively import a stack trace from a "remote" exception.  So,
//                //  move the _stackTraceString into the _remoteStackTraceString.  Note
//                //  that if there is an existing _remoteStackTraceString, it will be 
//                //  preserved at the head of the new string, so everything works as 
//                //  expected.
//                // Even if this exception is NOT thrown, things will still work as expected
//                //  because the StackTrace property returns the concatenation of the
//                //  _remoteStackTraceString and the _stackTraceString.
//                _remoteStackTraceString = _remoteStackTraceString + _stackTraceString;
//                _stackTraceString = null;
//            }
//#endif
//			SetData(e, "_remoteStackTraceString", st + rst);
//			//SetData(e, "_stackTraceString", null); no need to set to null
//		}

		//private static object GetValueNoThrow(SerializationInfo info, string field, Type t)
		//{
		//	info.AddSafeValue
		//}

		//private static void SetDataF(Exception e, string field, Func<object?> value)
		//{
		//	try
		//	{
		//		var msgField = typeof(Exception).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
		//		msgField.SetValue(e, value());
		//	}
		//	catch { }
		//}
		//private static void SetData(Exception e, string field, object? value)
		//{
		//	try
		//	{
		//		var msgField = typeof(Exception).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
		//		msgField.SetValue(e, value);
		//	}
		//	catch { }
		//}

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
			Type exceptionType = exception.GetType();
			//EnsureSerializableAttribute(exceptionType);
			exception.GetObjectData(info, Context);

			info.AddValue(AssemblyNameKeyName, exception.GetType().Assembly.FullName);

			if (exception.InnerException != null)
				info.AddValue("InnerExceptionString", exception.InnerException.ToString());
		}

		//internal static bool IsSerializable(Exception exception) => exception.GetType().GetCustomAttribute<SerializableAttribute>() is object;

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
				TypeCode.String => formatterConverter.ToString(value)!,
				TypeCode.UInt16 => formatterConverter.ToUInt16(value),
				TypeCode.UInt32 => formatterConverter.ToUInt32(value),
				TypeCode.UInt64 => formatterConverter.ToUInt64(value),
				_ => throw new NotSupportedException("Unsupported type code: " + typeCode),
			};
		}

		//// <summary>
		//// Gets a value like <see cref="SerializationInfo.MemberCount"/>
		//// but omits members that should not be serialized.
		//// </summary>
		//internal static int GetSafeMemberCount(this SerializationInfo info) => info.GetSafeMembers().Count();

		/// <summary>
		/// Gets a member enumerator that omits members that should not be serialized.
		/// </summary>
		internal static IEnumerable<SerializationEntry> GetSafeMembers(this SerializationInfo info)
		{
			foreach (SerializationEntry element in info)
			{
				if (element.Name == WatsonBucketsKey)
				{
					// skip
				}
				//else if (element.Name == "InnerException")
				//{
				//	yield return element;
				//}
				else
				{
					yield return element;
				}
			}
		}

		/// <summary>
		/// Adds a member if it isn't among those that should not be deserialized.
		/// </summary>
		internal static void AddSafeValue(this SerializationInfo info, string name, object? value)
		{
			if (name == WatsonBucketsKey)
			{ 
				// may be missing
			}
			else if (name == "InnerException")
			{
				// ignore
				info.AddValue(name, null);
			}
			else
			{
				info.AddValue(name, value);
			}
		}

		//private static void EnsureSerializableAttribute(Type runtimeType)
		//{
		//	if (runtimeType.GetCustomAttribute<SerializableAttribute>() is null)
		//	{
		//		throw new NotSupportedException($"{runtimeType.FullName} is not marked with the {typeof(SerializableAttribute).FullName}.");
		//	}
		//}

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
	}

}
