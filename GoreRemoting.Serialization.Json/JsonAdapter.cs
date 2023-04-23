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

	public class JsonAdapter : ISerializerAdapter
	{
		public JsonSerializerOptions Options { get; }

		public ExceptionMarshalStrategy ExceptionMarshalStrategy { get; set; } = ExceptionMarshalStrategy.BinaryFormatter;

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


		class ExceptionWrapper
		{
			public string Message { get; set; }
			public string StackTrace { get; set; }
			public string TypeName { get; set; }
			public string ClassName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
		}

		public object GetSerializableException(Exception ex)
		{
			if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.BinaryFormatter)
			{
				return _bf.GetExceptionData(ex);
			}
			else if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.UninitializedObject)
			{
				var ed = ExceptionSerializationHelpers.GetExceptionData(ex);
				return new ExceptionWrapper
				{
					Message = ed.Message,
					ClassName = ed.ClassName,
					StackTrace = ed.StackTrace,
					TypeName = ed.TypeName,
					PropertyData = ed.PropertyData
				};
			}
			else if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.RemoteInvocationException)
			{
				var ed = ExceptionSerializationHelpers.GetExceptionData(ex);
				return new ExceptionWrapper
				{
					Message = ed.Message,
					ClassName = ed.ClassName,
					StackTrace = ed.StackTrace,
					TypeName = ed.TypeName,
					PropertyData = ed.PropertyData
				};
			}
			else
				throw new NotSupportedException(ExceptionMarshalStrategy.ToString());
		}

		public Exception RestoreSerializedException(object ex)
		{
			if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.BinaryFormatter)
			{
				return _bf.RestoreException((byte[])ex);
			}
			else if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.UninitializedObject)
			{
				var ew = (ExceptionWrapper)ex;
				return ExceptionSerializationHelpers.RestoreWithGetUninitializedObject(new ExceptionData
				{
					Message = ew.Message,
					ClassName = ew.ClassName,
					TypeName = ew.TypeName,
					StackTrace = ew.StackTrace,
					PropertyData = ew.PropertyData
				});
			}
			else if (ExceptionMarshalStrategy == ExceptionMarshalStrategy.RemoteInvocationException)
			{
				var ew = (ExceptionWrapper)ex;
				return ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(new ExceptionData
				{
					Message = ew.Message,
					ClassName = ew.ClassName,
					TypeName = ew.TypeName,
					StackTrace = ew.StackTrace,
					PropertyData = ew.PropertyData
				});
			}
			else
				throw new NotSupportedException(ExceptionMarshalStrategy.ToString());
		}

		public string Name => "Json";
	}


}
