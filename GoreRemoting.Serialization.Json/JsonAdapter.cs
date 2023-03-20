using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TupleAsJsonArray;

namespace GoreRemoting.Serialization.Json
{
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
			TypeAndObject[] typeAndObjects = new TypeAndObject[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];

				if (obj != null)
				{
					var t = obj.GetType();
					var tao = new TypeAndObject() { TypeName = TypeShortener.GetShortType(t), Data = obj };
					typeAndObjects[i] = tao;
				}
				else
					typeAndObjects[i] = null;
			}

			System.Text.Json.JsonSerializer.Serialize<TypeAndObject[]>(stream, typeAndObjects, Options);
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			var typeAndObjects = JsonSerializer.Deserialize<TypeAndObject[]>(stream, Options)!;

			object[] res = new object[typeAndObjects.Length];

			for (int i = 0; i < typeAndObjects.Length; i++)
			{
				var to = typeAndObjects[i];
				if (to != null)
				{
					var t = Type.GetType(to.TypeName);
					res[i] = ((System.Text.Json.JsonElement)to.Data).Deserialize(t, Options);
				}
				else
					res[i] = null;
			}

			return res;
			//// maybe we can convert to same type as parameters?
			////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
			//	throw new NotImplementedException();
		}

		class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
		{
			public string? TypeName { get; set; }
			public string? Message { get; set; }
			public string? StackTrace { get; set; }

			public ExceptionWrapper()
			{

			}

			public ExceptionWrapper(Exception ex)
			{
				TypeName = TypeShortener.GetShortType(ex.GetType());
				Message = ex.Message;
				StackTrace = ex.StackTrace;
			}
		}

		public object GetSerializableException(Exception ex)
		{
			return new ExceptionWrapper(ex);
		}

		public Exception RestoreSerializedException(object ex)
		{
			var e = (ExceptionWrapper)ex;
			var type = Type.GetType(e.TypeName);

			Exception res = null;
			if (type != null)
			{
				res = ExceptionHelper.ConstructException(e.Message, type);
			}

			if (res == null)
			{
				res = new RemoteInvocationException(e.Message!, e.TypeName);
			}

			// set stack
			FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
			remoteStackTraceString.SetValue(res, e.StackTrace);
			remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);

			return res;
		}



		public string Name => "Json";
	}

	class TypeAndObject
	{
		public string TypeName { get; set; }
		public object Data { get; set; }
	}
}
