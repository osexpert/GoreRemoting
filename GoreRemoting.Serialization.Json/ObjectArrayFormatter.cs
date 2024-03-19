#if false

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoreRemoting.Serialization.Json
{
	public class ObjectArrayFormatter : JsonConverter<object?[]>
	{

		public override object?[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			try
			{
				var types = JsonAdapter._asyncLocalTypes.Value!;
				if (types == null)
					throw new Exception("types are null");

				object?[] res = new object[types.Length];

				int i = 0;

				if (reader.TokenType != JsonTokenType.StartObject)
					throw new Exception("not StartObject");

				if (!reader.Read())
					throw new Exception("not read 1");

				if (reader.TokenType != JsonTokenType.PropertyName)
					throw new Exception("not PropertyName");

				var argnam = reader.GetString();
				if (argnam != "Length")
					throw new Exception("not Length");

				if (!reader.Read())
					throw new Exception("not read 2");

				if (reader.TokenType != JsonTokenType.Number)
					throw new Exception("not Number");

				var len = reader.GetInt32();
				if (len != types.Length)
					throw new Exception("lengths mismatch");


				while (i < len)
				{
					if (!reader.Read())
						throw new Exception("not read 3");

					if (reader.TokenType != JsonTokenType.PropertyName)
						throw new Exception("not PropertyName2");

					var argnam2 = reader.GetString()!;
					// Arg#
					if (!argnam2.StartsWith("Arg"))
						throw new Exception("not Arg#");

					if (!reader.Read())
						throw new Exception("not read 4");


					var res_A = JsonSerializer.Deserialize(ref reader, types[i], options);
					res[i] = res_A;

					i++;

					//	if (i < len)
					//	goto next;
				}

				if (!reader.Read())
					throw new Exception("not read 5");

				if (reader.TokenType != JsonTokenType.EndObject)
					throw new Exception("not EndObject");


				return res;

				//// https://gmanvel.medium.com/system-text-json-jsonexception-read-too-much-or-not-enough-61a15952af5d
				//while (!(reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == originalDepth))
				//	if (!reader.Read())
				//		throw new Exception("not read 5");

				//return res;
			}
			finally
			{
				JsonAdapter._asyncLocalTypes.Value = null;
			}
		}

		public override void Write(Utf8JsonWriter writer, object?[] values, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			writer.WriteNumber("Length", values.Length);

			int i = 0;
			foreach (var v in values)
			{
				writer.WritePropertyName("Arg" + i);
				JsonSerializer.Serialize(writer, v, options);
			}

			writer.WriteEndObject();
		}
	}

	//public class ObjectFormatter : JsonConverter<object>
	//{

	//	public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	//	{
	//		if (reader.TokenType != JsonTokenType.StartObject)
	//			throw new Exception("not StartObject");

	//		var originalDepth = reader.CurrentDepth;

	//		if (!reader.Read())
	//			throw new Exception("not read 1");

	//		if (reader.TokenType != JsonTokenType.PropertyName)
	//			throw new Exception("not PropertyName");


	//		var propName = reader.GetString();
	//		if (propName != "type")
	//			throw new Exception("not type");

	//		if (!reader.Read())
	//			throw new Exception("not read 2");

	//		if (reader.TokenType != JsonTokenType.String)
	//			throw new Exception("not string");

	//		var typeName = reader.GetString() ?? throw new Exception("no typeName");

	//		var t = Type.GetType(typeName, true) ?? throw new Exception("no type");

	//		if (!reader.Read())
	//			throw new Exception("not read 3");

	//		//bool start = reader.TokenType == JsonTokenType.StartObject;

	//		if (reader.TokenType != JsonTokenType.PropertyName)
	//			throw new Exception("not PropertyName 2");

	//		//if (!reader.Read())
	//		//	throw new Exception("7.3");

	//		var propName2 = reader.GetString();
	//		if (propName2 != "data")
	//			throw new Exception("not data");

	//		// move to data
	//		if (!reader.Read())
	//			throw new Exception("not read 4");

	//		//var tokTypeBefore = reader.TokenType;

	//		var res = JsonSerializer.Deserialize(ref reader, t, options);

	//		//if (reader.TokenType != JsonTokenType.EndObject)
	//		//	throw new Exception("8");

	//		//if (!reader.Read())
	//		//	throw new Exception("9");


	//		// https://gmanvel.medium.com/system-text-json-jsonexception-read-too-much-or-not-enough-61a15952af5d
	//		while (!(reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == originalDepth))
	//			if (!reader.Read())
	//				throw new Exception("not read 5");

	//		return res;
	//	}

	//	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	//	{
	//		writer.WriteStartObject();

	//		writer.WriteString("type", TypeShortener.GetShortType(value.GetType()));

	//		writer.WritePropertyName("data");
	//		JsonSerializer.Serialize(writer, value, options);

	//		writer.WriteEndObject();
	//	}
	//}
}
#endif