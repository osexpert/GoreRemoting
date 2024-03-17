using MessagePack;
using MessagePack.Formatters;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		public ExceptionFormatStrategy ExceptionStrategy { get; set; } = ExceptionFormatStrategy.UninitializedObject;

		public MessagePackSerializerOptions? Options { get; set; } = null;

		public void Serialize(Stream stream, object?[] graph)
		{
			//Datas[] typeAndObjects = new Datas[graph.Length];
			//for (int i = 0; i < graph.Length; i++)
			//{
			//	var obj = graph[i];
			//	typeAndObjects[i] = new Datas { Data = obj };
			//}
			MessagePackSerializer.Serialize(stream, graph, Options);
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			object?[] res = new object[types.Length];

			using var r = new MessagePackStreamReader(stream, true);

			var ae = r.ReadArrayAsync(CancellationToken.None).GetAsyncEnumerator();
			try
			{
				int i = 0;
				while (ae.MoveNextAsync().GetAwaiter().GetResult())
				{
					var c = ae.Current;

					// problem: if we supported references, we would have lost references in the other arguments (since we desser every arg. individually but serialize together)
					// So...
					res[i] = MessagePackSerializer.Deserialize(types[i], c, Options);
					i++;
				}
			}
			finally
			{
				ae.DisposeAsync().GetAwaiter().GetResult();
			}

			//var typeAndObjects = MessagePackSerializer.Deserialize<Datas[]>(stream, Options)!;
			//object?[] res = new object?[typeAndObjects.Length];
			//for (int i = 0; i < typeAndObjects.Length; i++)
			//{
			//	var to = typeAndObjects[i];
			//	res[i] = to.Data;
			//}
			//return res;
			return res;
		}

		//[MessagePackObject]
		//public class Datas
		//{
		//	[Key(0)]
		//	[MessagePackFormatter(typeof(TypelessFormatter))]
		//	public object? Data { get; set; }
		//}

		public object GetSerializableException(Exception ex)
		{
			if (ExceptionStrategy == ExceptionFormatStrategy.UninitializedObject)
				return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex), ExceptionFormat.UninitializedObject);
			else if (ExceptionStrategy == ExceptionFormatStrategy.RemoteInvocationException)
				return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex), ExceptionFormat.RemoteInvocationException);
			else
				throw new NotSupportedException(ExceptionStrategy.ToString());
		}

		public Exception RestoreSerializedException(object ex)
		{
			var ew = (ExceptionWrapper)ex;
			return ew.Format switch
			{
				// TODO: add exception allow list, use Type.ToString() as lookup
				ExceptionFormat.UninitializedObject => ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew), Type.GetType(ew.TypeName)),
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
			return value;
		}

		public Type ExceptionType => typeof(ExceptionWrapper);


		[MessagePackObject(true)]
		public class ExceptionWrapper
		{
			public string TypeName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
			public byte[] BinaryFormatterData { get; set; }
			public ExceptionFormat Format { get; set; }
		}

	}

	public enum ExceptionFormatStrategy
	{
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
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 2,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 3
	}
}
