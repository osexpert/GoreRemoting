using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

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
			SerializationInfo info = null;

			if (ex.GetType().GetCustomAttribute<SerializableAttribute>() != null)
			{
				info = new SerializationInfo(ex.GetType(), new MessagePackFormatterConverter(Options));

				try
				{
					ExceptionSerializationHelpers.Serialize(ex, info);
				}
				catch
				{
					// cannot serialize for some reason
					info = null;
				}
			}

			return new ExceptionWrapper(ex, info);
		}

		public Exception RestoreSerializedException(object ex)
		{
			//	return (Exception)ex;

			var e = (ExceptionWrapper)ex;
			var type = Type.GetType(e.TypeName);

			Exception res = null;

			SerializationInfo info = null;
			if (e.HasSerializationInfo)
			{
				info = new SerializationInfo(type, new MessagePackFormatterConverter(Options));

				foreach (var kv in e.SerializationInfo)
					info.AddValue(kv.Key, kv.Value);

				try
				{
					res = ExceptionSerializationHelpers.Deserialize<Exception>(info, null);// this.formatter.rpc?.TraceSource);

					FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
					//				remoteStackTraceString.SetValue(res, e.StackTrace);
					remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
				}
				catch
				{
					// cannot deserialize for some reason
					res = null;
				}
			}


			if (res == null)
			{
				res = new RemoteInvocationException(e.Message!, e.TypeName);

				FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
				remoteStackTraceString.SetValue(res, e.StackTrace);
				remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
			}



			return res;

		}

		[MessagePackObject]
		public class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
		{
			[Key(0)]
			public string? TypeName { get; set; }
			[Key(1)]
			public string? Message { get; set; }
			[Key(2)]
			public string? StackTrace { get; set; }

			[Key(3)]
			public Dictionary<string, object> SerializationInfo { get; set; }
			[Key(4)]
			public bool HasSerializationInfo { get; set; }

			public ExceptionWrapper()
			{

			}

			public ExceptionWrapper(Exception ex, SerializationInfo info)
			{
				TypeName = TypeShortener.GetShortType(ex.GetType());
				Message = ex.Message;
				StackTrace = ex.StackTrace;

				if (info != null)
				{
					HasSerializationInfo = true;
					SerializationInfo = new();
					foreach (SerializationEntry se in info)
					{
						SerializationInfo.Add(se.Name, se.Value);
					}
				}
			}
		}


	}
}
