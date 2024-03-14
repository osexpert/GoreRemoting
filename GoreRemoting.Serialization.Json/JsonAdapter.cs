using System.Text.Json;
using System.Text.Json.Serialization;
using TupleAsJsonArray;

namespace GoreRemoting.Serialization.Json
{

	public class JsonAdapter : ISerializerAdapter
	{
		public JsonSerializerOptions Options { get; }

		public ExceptionFormatStrategy ExceptionStrategy { get; set; } = ExceptionFormatStrategy.UninitializedObject;
			//= ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject;

		//readonly Lazy<BinaryFormatterAdapter> _bfa = new(() => new());

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
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object?[] graph)
		{
			Dictionary<int, byte[]> byteArrays = new();
			object?[] objects = new object[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];
				if (obj is byte[] bs)
				{
					byteArrays.Add(i, bs);
				}
				else
				{
					objects[i] = obj;
				}
			}

			var bw = new GoreBinaryWriter(stream);
			bw.WriteVarInt(byteArrays.Count);
			foreach (var byteArray in byteArrays)
			{
				bw.WriteVarInt(byteArray.Key);
				bw.WriteVarInt(byteArray.Value.Length);
				bw.Write(byteArray.Value);
			}

			JsonSerializer.Serialize<object?[]>(stream, objects, Options);
		}

		//class ByteArray
		//{
		//	public int Idx;
		//	public byte[] Bytes;

		//	public ByteArray(int idx, byte[] bytes)
		//	{
		//		Idx = idx;
		//		Bytes = bytes;
		//	}
		//}


		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <returns>Deserialized object graph</returns>
		public object?[] Deserialize(Stream stream)
		{
			Dictionary<int, byte[]> byteArrays = new();

			var br = new GoreBinaryReader(stream);
			var bCnt = br.ReadVarInt();
			while (bCnt-- > 0)
			{
				var idx = br.ReadVarInt();
				var byteLen = br.ReadVarInt();
				var bytes = br.ReadBytes(byteLen);
				byteArrays.Add(idx, bytes);
			}

			var objects = JsonSerializer.Deserialize<object[]>(stream, Options)!;

			object?[] res = new object?[objects.Length];

			for (int i = 0; i < objects.Length; i++)
			{
				var to = objects[i];
				if (byteArrays.TryGetValue(i, out var bytes))
				{
					if (to != null)
						throw new Exception("sanity: to should be null");
					res[i] = bytes;
				}
				else
				{
					res[i] = to;
				}
			}

			return res;

			//// maybe we can convert to same type as parameters?
			////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
			//	throw new NotImplementedException();
		}


		class ExceptionWrapper
		{
			public ExceptionFormat Format { get; set; }
			public byte[] BinaryFormatterData { get; set; }
			public string TypeName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
		}

		public object GetSerializableException(Exception ex)
		{
			//if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject ||
			//				ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrRemoteInvocationException)
			//{
			//	try
			//	{
			//		// INFO: even if this is true, serialization may fail based on what is put in the Data-dictionary etc.
			//		if (ex.GetType().IsSerializable)
			//			return new ExceptionWrapper { Format = ExceptionFormat.BinaryFormatter, BinaryFormatterData = _bfa.Value.GetExceptionData(ex) };
			//	}
			//	catch
			//	{ }

			//	var ed = ExceptionSerializationHelpers.GetExceptionData(ex);
			//	if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject)
			//		return ToExceptionWrapper(ed, ExceptionFormat.UninitializedObject);
			//	else if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrRemoteInvocationException)
			//		return ToExceptionWrapper(ed, ExceptionFormat.RemoteInvocationException);
			//	else
			//		throw new NotSupportedException(ExceptionStrategy.ToString());
			//}
			//else 
			if (ExceptionStrategy == ExceptionFormatStrategy.UninitializedObject)
				return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex), ExceptionFormat.UninitializedObject);
			else if (ExceptionStrategy == ExceptionFormatStrategy.RemoteInvocationException)
				return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex), ExceptionFormat.RemoteInvocationException);
			else
				throw new NotSupportedException(ExceptionStrategy.ToString());
		}

		public Exception RestoreSerializedException(object ex)
		{
			var ew = (ExceptionWrapper)Deserialize(typeof(ExceptionWrapper), ex)!;
			return ew.Format switch
			{
				//ExceptionFormat.BinaryFormatter => _bfa.Value.RestoreException(ew.BinaryFormatterData),
				ExceptionFormat.UninitializedObject => ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew)),
				ExceptionFormat.RemoteInvocationException => ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(ToExceptionData(ew)),
				_ => throw new NotSupportedException(ew.Format.ToString())
			};
		}

		private static ExceptionWrapper ToExceptionWrapper(ExceptionData ed, ExceptionFormat format)
		{
			return new ExceptionWrapper
			{
				TypeName = ed.TypeName,
				PropertyData = ed.PropertyData,
				Format = format
			};
		}

		private static ExceptionData ToExceptionData(ExceptionWrapper ew)
		{
			return new ExceptionData
			{
				TypeName = ew.TypeName,
				PropertyData = ew.PropertyData
			};
		}

		public object? Deserialize(Type type, object? value)
		{
			if (value is JsonElement je)
				return je.Deserialize(type, Options);
			else
				return value;
		}

		public string Name => "Json";
	}

	public enum ExceptionFormatStrategy
	{
	//	/// <summary>
	//	/// BinaryFormatter used (if serializable, everything is preserved, else serialized as UninitializedObject)
	//	/// </summary>
	//	BinaryFormatterOrUninitializedObject = 1,
	//	/// <summary>
	//	/// BinaryFormatter used (if serializable, everything is preserved, else serialized as RemoteInvocationException)
	//	/// </summary>
	//	BinaryFormatterOrRemoteInvocationException = 2,
		/// <summary>
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 3,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 4
	}

	public enum ExceptionFormat
	{
		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved, else serialized as UninitializedObject)
		/// </summary>
		//BinaryFormatter = 1,
		/// <summary>
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 2,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 3
	}
}
