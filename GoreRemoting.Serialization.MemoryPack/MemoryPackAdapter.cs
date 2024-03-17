using System;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	public class MemoryPackAdapter : ISerializerAdapter
	{
		public MemoryPackSerializerOptions? Options { get; set; }

		public ExceptionFormatStrategy ExceptionStrategy { get; set; } = ExceptionFormatStrategy.UninitializedObject;

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object?[] graph)
		{
			MemoryPackSerializer.SerializeAsync<MemPackObjectArray>(stream, new MemPackObjectArray { Datas = graph }, Options).GetAwaiter().GetResult();
		}

		internal static AsyncLocal<Type[]?> _asyncLocalTypes = new AsyncLocal<Type[]?>();

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <returns>Deserialized object graph</returns>
		public object?[] Deserialize(Stream stream, Type[] types)
		{
			

			try
			{
				if (_asyncLocalTypes.Value != null)
					throw new Exception("already set");
				_asyncLocalTypes.Value = types;

				//			long pos = stream.Position;

				var res = MemoryPackSerializer.DeserializeAsync<MemPackObjectArray>(stream, Options).GetAwaiter().GetResult()!.Datas;
				return res;
			}
			finally
			{
				_asyncLocalTypes.Value = null;
			}

			
		}

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

		public string Name => "MemoryPack";

		public Type ExceptionType => typeof(ExceptionWrapper);
	}

	[MemoryPackable]
	public partial class ExceptionWrapper
	{
		public ExceptionFormat Format { get; set; }
		public byte[] BinaryFormatterData { get; set; }
		public string TypeName { get; set; }
		public Dictionary<string, string> PropertyData { get; set; }
	}

	[MemoryPackable]
	partial class MemPackObjectArray
	{
		[ObjectArrayFormatter]
		public object?[] Datas { get; set; } = null!;
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
