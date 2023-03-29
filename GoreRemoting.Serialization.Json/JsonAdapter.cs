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

	public class TypelessFormatter : JsonConverter<object>
	{
		// WHAT ISTHE POINT OF THIS?
		//public override bool HandleNull => true;

	//	public override bool HandleNull => true;//base.HandleNull;

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
					new TupleConverterFactory(),
					//new FixedVersionConverter()
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
				//if (obj != null)
				//{
				//	var t = obj.GetType();
				//	var tao = new TypeAndObject() { TypeName = TypeShortener.GetShortType(t), Data = obj };
				//	typeAndObjects[i] = tao;
				//}
				//else
				//	typeAndObjects[i] = null;
			}

			System.Text.Json.JsonSerializer.Serialize<ObjectOnly[]>(stream, typeAndObjects, Options);

			//TypeAndObject[] typeAndObjects = new TypeAndObject[graph.Length];

			//for (int i = 0; i < graph.Length; i++)
			//{
			//	var obj = graph[i];

			//	if (obj != null)
			//	{
			//		var t = obj.GetType();
			//		var tao = new TypeAndObject() { TypeName = TypeShortener.GetShortType(t), Data = obj };
			//		typeAndObjects[i] = tao;
			//	}
			//	else
			//		typeAndObjects[i] = null;
			//}

			//System.Text.Json.JsonSerializer.Serialize<TypeAndObject[]>(stream, typeAndObjects, Options);



			//stream.Flush();
			//stream.Position = 0;
			//using (var fs = File.OpenWrite("e:\\fff.json"))
			//{
			//	stream.CopyTo(fs);
			//}
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
				//if (to != null)
				//{
				//	var t = Type.GetType(to.TypeName);
				//	res[i] = ((System.Text.Json.JsonElement)to.Data).Deserialize(t, Options);
				//}
				//else
				//	res[i] = null;
			}

			return res;


			//var typeAndObjects = JsonSerializer.Deserialize<TypeAndObject[]>(stream, Options)!;

			//object[] res = new object[typeAndObjects.Length];

			//for (int i = 0; i < typeAndObjects.Length; i++)
			//{
			//	var to = typeAndObjects[i];
			//	if (to != null)
			//	{
			//		var t = Type.GetType(to.TypeName);
			//		res[i] = ((System.Text.Json.JsonElement)to.Data).Deserialize(t, Options);
			//	}
			//	else
			//		res[i] = null;
			//}

			//return res;


			//// maybe we can convert to same type as parameters?
			////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
			//	throw new NotImplementedException();
		}

		class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
		{
			public string? TypeName { get; set; }
			public string? Message { get; set; }
			public string? StackTrace { get; set; }

			public Dictionary<string, object> SerializationInfo { get; set; }
			public bool HasSerializationInfo { get; set; }

			public ExceptionWrapper()
			{

			}

			public ExceptionWrapper(Exception ex, SerializationInfo info)
			{
				TypeName = TypeShortener.GetShortType(ex.GetType());
				Message = ex.Message;
				StackTrace = ex.StackTrace;

				if (info != null)
				{
					HasSerializationInfo = true;
					SerializationInfo = new();
					foreach (SerializationEntry se in info)
					{
						SerializationInfo.Add(se.Name, se.Value);
					}
				}
			}
		}

		public object GetSerializableException(Exception ex)
		{
			SerializationInfo info = null;

			if (ex.GetType().GetCustomAttribute<SerializableAttribute>() != null)
			{
				info = new SerializationInfo(ex.GetType(), new JsonConverterFormatter(Options));

				try
				{
					ExceptionSerializationHelpers.Serialize(ex, info);
				}
				catch
				{
					// cannot serialize for some reason
					info = null;
				}
			}

			return new ExceptionWrapper(ex, info);
		}

		public Exception RestoreSerializedException(object ex)
		{
			var e = (ExceptionWrapper)ex;
			var type = Type.GetType(e.TypeName);

			Exception res = null;

			if (e.HasSerializationInfo)
			{
				var info = new SerializationInfo(type, new JsonConverterFormatter(Options));

				foreach (var kv in e.SerializationInfo)
					info.AddValue(kv.Key, kv.Value);

				try
				{
					res = ExceptionSerializationHelpers.Deserialize<Exception>(info, null);// this.formatter.rpc?.TraceSource);

					FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
					//				remoteStackTraceString.SetValue(res, e.StackTrace);
					remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
				}
				catch
				{
					// cannot deserialize for some reason
					res = null;
				}
			}


			if (res == null)
			{
				res = new RemoteInvocationException(e.Message!, e.TypeName);

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
		}



		public string Name => "Json";
	}

	class TypeAndObject
	{
		public string TypeName { get; set; }
		public object Data { get; set; }
	}

	class JsonConverterFormatter : IFormatterConverter
	{
		private readonly JsonSerializerOptions _options;

		internal JsonConverterFormatter(JsonSerializerOptions options)//JsonSerializer serializer)
		{
			_options = options;
		}

		public object Convert(object value, Type type) => ((JsonElement)value).Deserialize(type, this._options);

		public object Convert(object value, TypeCode typeCode)
		{
			return typeCode switch
			{
				TypeCode.Object => ((JsonElement)value).Deserialize(typeof(object), this._options),
				_ => ExceptionSerializationHelpers.Convert(this, value, typeCode),
			};
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

	//public class FixedVersionConverter : JsonConverter<Version>
	//{
	//	private readonly static JsonConverter<Version> s_defaultConverter =
	//		(JsonConverter<Version>)JsonSerializerOptions.Default.GetConverter(typeof(Version));

 //       public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	//	{
	//		if (reader.TokenType == JsonTokenType.Null)
	//			return null;

	//		return s_defaultConverter.Read(ref reader, typeToConvert, options);
	//	}

	//	public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
	//	{
	//		if (value == null)
	//			writer.WriteNullValue();
	//		else
	//			s_defaultConverter.Write(writer, value, options);
	//	}
	//}
}
