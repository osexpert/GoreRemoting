using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using GoreRemoting.Serialization.BinaryFormatter;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		BinaryFormatterAdapter _bf = new();

		public ExceptionFormatStrategy ExceptionStrategy { get; set; } = ExceptionFormatStrategy.BinaryFormatterOrUninitializedObject;

		public MessagePackSerializerOptions Options { get; set; } = null;

		public void Serialize(Stream stream, object[] graph)
		{
			Datas[] typeAndObjects = new Datas[graph.Length];
			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];
				typeAndObjects[i] = new Datas { Data = obj };
			}
			MessagePackSerializer.Serialize<Datas[]>(stream, typeAndObjects, Options);
		}

		public object[] Deserialize(Stream stream)
		{
			var typeAndObjects = MessagePackSerializer.Deserialize<Datas[]>(stream, Options)!;
			object[] res = new object[typeAndObjects.Length];
			for (int i = 0; i < typeAndObjects.Length; i++)
			{
				var to = typeAndObjects[i];
				res[i] = to.Data;
			}
			return res;
		}

		[MessagePackObject]
		public class Datas
		{
			[Key(0)]
			[MessagePackFormatter(typeof(TypelessFormatter))]
			public object Data { get; set; }
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
				Message = ed.Message,
				ClassName = ed.ClassName,
				StackTrace = ed.StackTrace,
				TypeName = ed.TypeName,
				PropertyData = ed.PropertyData,
				Format = format
			};
		}

		private static ExceptionData ToExceptionData(ExceptionWrapper ew)
		{
			return new ExceptionData
			{
				Message = ew.Message,
				ClassName = ew.ClassName,
				StackTrace = ew.StackTrace,
				TypeName = ew.TypeName,
				PropertyData = ew.PropertyData
			};
		}

		[MessagePackObject]
		public class ExceptionWrapper
		{
			[Key(0)]
			public string TypeName { get; set; }
			[Key(1)]
			public string Message { get; set; }
			[Key(2)]
			public string StackTrace { get; set; }
			[Key(3)]
			public string ClassName { get; set; }
			[Key(4)]
			public Dictionary<string, string> PropertyData { get; set; }
			[Key(5)]
			public byte[] BinaryFormatterData { get; set; }
			[Key(6)]
			public ExceptionFormat Format { get; set; }

		}

	}
}
