using GoreRemoting.Serialization.BinaryFormatter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TupleAsJsonArray;

namespace GoreRemoting.Serialization.Json
{

	//public class FailsafeFormatter : JsonConverter<Dictionary<string, object>>
	//{
	//	public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	//	{
	//		// this will always succeed i think, because we do not resolve types here, it will be left as object, whatever it contains
	//		return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
	//	}

	//	public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
	//	{
	//		foreach (var kv in value)
	//		{
	//			writer.WritePropertyName(kv.Key);

	//			if (kv.Value == null)
	//			{
	//				writer.WriteNullValue();
	//			}
	//			else
	//			{
	//				// what input type to use???
	//				byte[] bytes = null;
	//				try
	//				{
	//					bytes = JsonSerializer.SerializeToUtf8Bytes(kv.Value, kv.Value.GetType(), options);

	//				}
	//				catch
	//				{
	//				}

	//				if (bytes != null)
	//					writer.WriteRawValue(bytes);
	//				else
	//				{
	//					// write null
	//					writer.WriteNullValue();
	//				}


	//				//JsonSerializer.ser
	//			}
	//		}
	//	}
	//}


	public class TypelessFormatter : JsonConverter<object>
	{

		public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new Exception("not StartObject");

			var originalDepth = reader.CurrentDepth;

			if (!reader.Read())
				throw new Exception("not read 1");

			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new Exception("not PropertyName");


			var propName = reader.GetString();
			if (propName != "type")
				throw new Exception("not type");

			if (!reader.Read())
				throw new Exception("not read 2");

			if (reader.TokenType != JsonTokenType.String)
				throw new Exception("not string");

			var typeName = reader.GetString();

			var t = Type.GetType(typeName, true);

			if (!reader.Read())
				throw new Exception("not read 3");

			//bool start = reader.TokenType == JsonTokenType.StartObject;

			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new Exception("not PropertyName 2");

			//if (!reader.Read())
			//	throw new Exception("7.3");

			var propName2 = reader.GetString();
			if (propName2 != "data")
				throw new Exception("not data");

			// move to data
			if (!reader.Read())
				throw new Exception("not read 4");

			//var tokTypeBefore = reader.TokenType;

			var res = JsonSerializer.Deserialize(ref reader, t, options);

			//if (reader.TokenType != JsonTokenType.EndObject)
			//	throw new Exception("8");

			//if (!reader.Read())
			//	throw new Exception("9");


			// https://gmanvel.medium.com/system-text-json-jsonexception-read-too-much-or-not-enough-61a15952af5d
			while (!(reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == originalDepth))
				if (!reader.Read())
					throw new Exception("not read 5");

			return res;
		}

		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			writer.WriteString("type", TypeShortener.GetShortType(value.GetType()));

			writer.WritePropertyName("data");
			JsonSerializer.Serialize(writer, value, options);

			writer.WriteEndObject();
		}
	}

	public class JsonAdapter : ISerializerAdapter
	{
		public JsonSerializerOptions Options { get; }

		BinaryFormatterAdapter _bf = new();

		public JsonAdapter()
        {
			Options = CreateOptions();

		}

		private static JsonSerializerOptions CreateOptions()
		{
			return new JsonSerializerOptions()
			{
				IncludeFields = true,
				ReferenceHandler = ReferenceHandler.Preserve,
				Converters =
				{
					new TupleConverterFactory()
				}

			};
		}

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object[] graph)
		{
			ObjectOnly[] typeAndObjects = new ObjectOnly[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];

				typeAndObjects[i] = new ObjectOnly { Data = obj };
			}

			JsonSerializer.Serialize<ObjectOnly[]>(stream, typeAndObjects, Options);
		}

		class ObjectOnly
		{
			[JsonConverter(typeof(TypelessFormatter))]
			public object Data { get; set; }
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			var typeAndObjects = JsonSerializer.Deserialize<ObjectOnly[]>(stream, Options)!;

			object[] res = new object[typeAndObjects.Length];

			for (int i = 0; i < typeAndObjects.Length; i++)
			{
				var to = typeAndObjects[i];
				res[i] = to.Data;
			}

			return res;

			//// maybe we can convert to same type as parameters?
			////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
			//	throw new NotImplementedException();
		}

		//class ExceptionWrapper2
		//{
		//	public byte[] BinaryFormatterData { get; set; }
		//}

#if false
		class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
		{
			public string? TypeName { get; set; }
			public string? ClassName { get; set; }

			public string? Message { get; set; }
			public string? StackTrace { get; set; }

			/// <summary>
			/// We always want serialization\desser of this to not fail.
			/// </summary>
//			[JsonConverter(typeof(FailsafeFormatter))]
			public Dictionary<string, object> SerializationInfo { get; set; }

			//public bool HasSerializationInfo { get; set; }

			public ExceptionWrapper()
			{

			}

			public ExceptionWrapper(Exception ex, Dictionary<string, object> info)
			{
				TypeName = TypeShortener.GetShortType(ex.GetType());
				ClassName = ex.GetType().ToString();
				Message = ex.Message;
				StackTrace = ex.StackTrace;

				SerializationInfo = info;
//				if (info != null)
//				{
////					HasSerializationInfo = true;
//					SerializationInfo = new();
//					foreach (SerializationEntry se in info)
//					{


//						SerializationInfo.Add(se.Name, se.Value);
//					}
				
			}
		}
#endif


		public object GetSerializableException(Exception ex)
		{
			// TODO: this can fail. Catch it and failover to a wrapped exception?

			//return new ExceptionWrapper2() { BinaryFormatterData = _bf.GetExceptionData(ex) };
			return _bf.GetExceptionData(ex);

			//SerializationInfo info = GetObjectData(ex);

			//Dictionary<string, object> b =  FilterInfo(info);

			//return new ExceptionWrapper(ex, b);
		}

#if false

		/// <summary>
		/// Filter out those that can't be serialized
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		private Dictionary<string, object> FilterInfo(SerializationInfo info)
		{
			Dictionary<string, object> res = new();

			foreach (SerializationEntry se in info)
			{
				try
				{
					if (se.Value != null)
					{
						// TODO: would be nice if we could keep this data...instad of waisting it
						var dummy = JsonSerializer.SerializeToUtf8Bytes(se.Value, se.Value.GetType(), Options);
						res.Add(se.Name, se.Value);
					}
					else
					{
						res.Add(se.Name, null);
					}
				}
				catch
				{
					res.TryAdd(se.Name, null);
				}
			}

			return res;
		}
#endif

#if false
		private SerializationInfo GetObjectData(Exception ex)
		{
			try
			{
				//if (ex.GetType().GetCustomAttribute<SerializableAttribute>() != null)
				{
					// BUT...is JsonConverterFormatter used for serializing???
					// use a fake converter here?
					

					// write exeption into info
					return ExceptionSerializationHelpers.GetObjectData(ex);
					//return info;
				}
			}
			catch
			{
				return null;
			}
		}
#endif

		public Exception RestoreSerializedException(object ex)
		{
			//var e = (ExceptionWrapper2)ex;

			return _bf.RestoreException((byte[])ex);

#if false
			var type = Type.GetType(e.TypeName);

			if (type != null)
			{
				if (!typeof(Exception).IsAssignableFrom(type))
				{
					throw new NotSupportedException($"{e.TypeName} does not derive from {typeof(Exception).FullName}.");
				}
			}

			Exception res = null;

			if (type != null)
			{
				var info = new SerializationInfo(type, new JsonConverterFormatter(Options));

				if (e.SerializationInfo != null)
					foreach (var kv in e.SerializationInfo)
						info.AddValue(kv.Key, kv.Value);

				try
				{
					res = ExceptionSerializationHelpers.DeserializingConstructor(type, info);
				}
				catch { }

				if (res != null)
				{
					FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
					//				remoteStackTraceString.SetValue(res, e.StackTrace);
					remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
				}
			}

			if (res == null && type != null)
			{
				// Use some strandard ctor
				res = ExceptionHelper.ConstructException(e.Message, type);

				FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
				remoteStackTraceString.SetValue(res, e.StackTrace);
				remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
			}

			if (res == null && type != null)
			{
				var info = new SerializationInfo(type, new JsonConverterFormatter(Options));

				if (e.SerializationInfo != null)
					foreach (var kv in e.SerializationInfo)
						info.AddValue(kv.Key, kv.Value);

				try
				{
					res = ExceptionSerializationHelpers.DeserializingConstructor(typeof(RemoteInvocationException), info);

				}
				catch { }
				//FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
				//remoteStackTraceString.SetValue(res, e.StackTrace);
				//remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
			}

			if (res == null)
			{
				res = new RemoteInvocationException(e.Message!, e.ClassName);

				FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
				remoteStackTraceString.SetValue(res, e.StackTrace);
				remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
			}



			//Exception res = null;
			//if (type != null)
			//{
			//	res = ExceptionHelper.ConstructException(e.Message, type);
			//}

			//if (res == null)
			//{
			//	res = new RemoteInvocationException(e.Message!, e.TypeName);
			//}

			// set stack (fixme: same for both variants above?? check!!)


			return res;
#endif
		}



		public string Name => "Json";
	}

	//class TypeAndObject
	//{
	//	public string TypeName { get; set; }
	//	public object Data { get; set; }
	//}


#if false
	class JsonConverterFormatter : IFormatterConverter
	{
		private readonly JsonSerializerOptions _options;

		internal JsonConverterFormatter(JsonSerializerOptions options)//JsonSerializer serializer)
		{
			_options = options;
		}

		public object Convert(object value, Type type)
		{
			try
			{
				return ((JsonElement)value).Deserialize(type, this._options);
			}
			catch
			{
				// ignore errors
				return null;
			}
		}

		public object Convert(object value, TypeCode typeCode)
		{
			if (typeCode == TypeCode.Object)
			{
				try
				{
					return ((JsonElement)value).Deserialize<object>(this._options);
				}
				catch
				{
					// ignore errors
					return null;
				}
			}
			else
			{
				return ExceptionSerializationHelpers.Convert(this, value, typeCode);
			}
		}

		public bool ToBoolean(object value) => ((JsonElement)value).Deserialize<bool>(this._options);

		public byte ToByte(object value) => ((JsonElement)value).Deserialize<byte>(this._options);

		public char ToChar(object value) => ((JsonElement)value).Deserialize<char>(this._options);

		public DateTime ToDateTime(object value) => ((JsonElement)value).Deserialize<DateTime>(this._options);

		public decimal ToDecimal(object value) => ((JsonElement)value).Deserialize<decimal>(this._options);

		public double ToDouble(object value) => ((JsonElement)value).Deserialize<double>(this._options);

		public short ToInt16(object value) => ((JsonElement)value).Deserialize<short>(this._options);

		public int ToInt32(object value) => ((JsonElement)value).Deserialize<int>(this._options);

		public long ToInt64(object value) => ((JsonElement)value).Deserialize<long>(this._options);

		public sbyte ToSByte(object value) => ((JsonElement)value).Deserialize<sbyte>(this._options);

		public float ToSingle(object value) => ((JsonElement)value).Deserialize<float>(this._options);

		public string ToString(object value) => ((JsonElement)value).Deserialize<string>(this._options);

		public ushort ToUInt16(object value) => ((JsonElement)value).Deserialize<ushort>(this._options);

		public uint ToUInt32(object value) => ((JsonElement)value).Deserialize<uint>(this._options);

		public ulong ToUInt64(object value) => ((JsonElement)value).Deserialize<ulong>(this._options);
	}

#endif
	

}
