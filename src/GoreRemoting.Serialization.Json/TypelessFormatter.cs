﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoreRemoting.Serialization.Json
{
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
}