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

		public ExceptionFormatStrategy ExceptionStrategy { get; set; } = ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject;

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
			if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject ||
							ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrRemoteInvocationException)
			{
				// INFO: even if this is true, serialization may fail based on what is put in the Data-dictionary etc.
				if (ex.GetType().IsSerializable)
					return new ExceptionWrapper { Format = ExceptionFormat.BinaryFormatter, BinaryFormatterData = _bf.GetExceptionData(ex) };

				var ed = ExceptionSerializationHelpers.GetExceptionData(ex);
				if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject)
					return ToExceptionWrapper(ed, ExceptionFormat.UninitializedObject);
				else if (ExceptionStrategy == ExceptionFormatStrategy.BinaryFormatterOrRemoteInvocationException)
					return ToExceptionWrapper(ed, ExceptionFormat.RemoteInvocationException);
				else
					throw new NotSupportedException(ExceptionStrategy.ToString());
			}
			else if (ExceptionStrategy == ExceptionFormatStrategy.UninitializedObject)
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
				ExceptionFormat.BinaryFormatter => _bf.RestoreException(ew.BinaryFormatterData),
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

		public string Name => "MemoryPack";
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
		[UnsafeObjectArrayFormatter]
		public object[] Datas { get; set; } = null!;
	}

}
