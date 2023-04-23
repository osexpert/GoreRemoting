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

		public ExceptionMarshalStrategy ExceptionMarshalStrategy { get; set; } = ExceptionMarshalStrategy.BinaryFormatter;
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
		}

	}
}
