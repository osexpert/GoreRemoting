using GoreRemoting.Serialization.BinaryFormatter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Castle.Components.DictionaryAdapter;
using MemoryPack;
using MemoryPack.Formatters;
using MemoryPack.Internal;

namespace GoreRemoting.Serialization.MemoryPack
{
	public class MemoryPackAdapter : ISerializerAdapter
	{

		public MemoryPackSerializerOptions Options { get; set; }

		BinaryFormatterAdapter _bf = new();

		public ExceptionMarshalStrategy ExceptionMarshalStrategy { get; set; } = ExceptionMarshalStrategy.BinaryFormatter;

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object[] graph)
		{
			MemoryPackSerializer.SerializeAsync<MemPackObjectArray>(stream, new MemPackObjectArray() { Datas = graph }, Options).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			return MemoryPackSerializer.DeserializeAsync<MemPackObjectArray>(stream, Options).GetAwaiter().GetResult()!.Datas;
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


		public string Name => "MemoryPack";
	}

	[MemoryPackable]
	public partial class ExceptionWrapper
	{
		public string ClassName { get; set; }
		public string TypeName { get; set; }
		public string Message { get; set; }
		public string StackTrace { get; set; }
		public Dictionary<string, string> PropertyData { get; set; }
	}

	[MemoryPackable]
	partial class MemPackObjectArray
	{
		[UnsafeObjectArrayFormatter]
		public object[] Datas { get; set; } = null!;
	}

}
