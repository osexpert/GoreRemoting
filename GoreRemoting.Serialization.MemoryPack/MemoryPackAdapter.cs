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
			//return new ExceptionWrapper2() { BinaryFormatterData = _bf.GetExceptionData(ex) };
			return _bf.GetExceptionData(ex);
#if false
			SerializationInfo info = null;

			//if (ex.GetType().GetCustomAttribute<SerializableAttribute>() != null)
			{
				//info = new SerializationInfo(ex.GetType(), new DummyConverterFormatter());

				try
				{
					info = ExceptionSerializationHelpers.GetObjectData(ex);
				}
				catch
				{
					// cannot serialize for some reason
					info = null;
				}
			}

			return new ExceptionWrapper(ex, info);
#endif
		}

		public Exception RestoreSerializedException(object ex)
		{

			//var e = (ExceptionWrapper2)ex;

			return _bf.RestoreException((byte[])ex);//.BinaryFormatterData);

#if false

			var e = (ExceptionWrapper)ex;
			var type = Type.GetType(e.TypeName);

			Exception res = null;


			if (e.HasSerializationInfo)
			{
				var info = new SerializationInfo(type, new MemoryPackFormatterConverter(Options));

				for (int i = 0; i < e.SerializationInfoNames.Length; i++)
				{
					var value = e.SerializationInfoValues[i];

					if (e.SerializationInfoNames[i] == "Data")
					{
						if (value is IDictionary<object, object> d)
						{
							
							var ld = new System.Collections.Specialized.ListDictionary();
							foreach (var kv in d)
							{
								ld.Add(kv.Key, kv.Value);
							}

							value = ld;
						}
					}

					//foreach (var kv in e.SerializationInfo)
					info.AddValue(e.SerializationInfoNames[i], value);
				}

				try
				{
					res = ExceptionSerializationHelpers.DeserializingConstructor(type, info);// this.formatter.rpc?.TraceSource);

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
				res = new RemoteInvocationException(e.Message!, e.ClassName);

				FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
				remoteStackTraceString.SetValue(res, e.StackTrace);
				remoteStackTraceString.SetValue(res, res.StackTrace + System.Environment.NewLine);
			}



			return res;
#endif
		}


		public string Name => "MemoryPack";
	}


	//[MemoryPackable]
	//public partial class ExceptionWrapper2 //: Exception //seems impossible that MemPack can inherit exception?
	//{
	//	public byte[] BinaryFormatterData { get; set; }
	//}

#if false
	[MemoryPackable]
	public partial class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
	{
		public string? ClassName { get; set; }
		public string? TypeName { get; set; }

		public string? Message { get; set; }

		public string? StackTrace { get; set; }

	
		public string[] SerializationInfoNames { get; set; }

		[SerInfoArrayFormatter]
		public object[] SerializationInfoValues { get; set; }
		//public Dictionary<string, object> SerializationInfo { get; set; }

		public bool HasSerializationInfo { get; set; }

		[MemoryPackConstructor]
		public ExceptionWrapper()
		{

		}

		public ExceptionWrapper(Exception ex, SerializationInfo info)
		{
			TypeName = TypeShortener.GetShortType(ex.GetType());
			ClassName = ex.GetType().ToString();
			Message = ex.Message;
			StackTrace = ex.StackTrace;

			if (info != null)
			{
				HasSerializationInfo = true;
				//SerializationInfo = new();

				SerializationInfoNames = new string[info.MemberCount];
				SerializationInfoValues = new object[info.MemberCount];

				int i = 0;
				foreach (SerializationEntry se in info)
				{
					//SerializationInfo.Add(se.Name, se.Value);

					var value = se.Value;

					if (se.Name == "Data")
					{
						if (value is IDictionary)
						{
							var d = new Dictionary<object, object>();
							foreach (DictionaryEntry de in (IDictionary)value)
								d.Add(de.Key, de.Value);

							value = d;
							//			return;
						}
					}


					SerializationInfoNames[i] = se.Name;
					SerializationInfoValues[i] = value;
					i++;
				}
			}
		}
	}
#endif



	[MemoryPackable]
	partial class MemPackObjectArray
	{
		[UnsafeObjectArrayFormatter]
		public object[] Datas { get; set; } = null!;
	}











}
