using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	public class MemoryPackAdapter : ISerializerAdapter
	{

		public MemoryPackSerializerOptions Options { get; set; } = null;

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
			return new ExceptionWrapper(ex);
		}

		public Exception RestoreSerializedException(object ex)
		{
			var e = (ExceptionWrapper)ex;
			var exeptionType = Type.GetType(e.TypeName);

			Exception res = null;
			if (exeptionType != null)
			{
				res = ExceptionHelper.ConstructException(e.Message, exeptionType);
			}

			if (res == null)			
			{
				res = new RemoteInvocationException(e.Message!, e.TypeName);
			}

			// set stack
			FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
			remoteStackTraceString.SetValue(res, e.StackTrace + System.Environment.NewLine);

			return res;
		}

		public string Name => "MemoryPack";
	}



	/// <summary>
	/// TODO: can this be used for anything?
	/// </summary>
	[MemoryPackable]
	public /*abstract*/ partial class MemoryPackException : Exception
	{
		[MemoryPackIgnore]
		public new MethodBase TargetSite { get; }

		[MemoryPackIgnore]
		public new IDictionary Data { get; }

		[MemoryPackIgnore]
		public new Exception InnerException { get; }

		[MemoryPackConstructor]
		public MemoryPackException(string message) : base(message)
        {
            
        }
    }




	[MemoryPackable]
	partial class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
	{
		public string? TypeName { get; set; }
		public string? Message { get; set; }
		public string? StackTrace { get; set; }

		[MemoryPackConstructor]
        public ExceptionWrapper()
        {
            
        }

		public ExceptionWrapper(Exception ex)
		{
			TypeName = TypeShortener.GetShortType(ex.GetType());
			Message = ex.Message;
			StackTrace = ex.StackTrace;
		}
	}

	[MemoryPackable]
	partial class MemPackObjectArray
	{
		[UnsafeObjectArrayFormatter]
		public object[] Datas { get; set; } = null!;
	}
}
