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

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream s, object[] graph)
		{
			MemoryPackSerializer.SerializeAsync<MemPackObjectArray>(s, new MemPackObjectArray() { Datas = graph }).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			return MemoryPackSerializer.DeserializeAsync<MemPackObjectArray>(stream).GetAwaiter().GetResult()!.Datas;
		}

		public object GetSerializableException(Exception ex2)
		{
			return new ExceptionWrapper(ex2);// return ex2.GetType().IsSerializable ? ex2 : new RemoteInvocationException(ex2.Message);
		}

		public Exception RestoreSerializedException(object ex2)
		{
			var e = (ExceptionWrapper)ex2;
			var t = Type.GetType(e.TypeName);

			Exception newE = null;
			if (t != null)
			{
				// can this fail? missing ctor? yes, can fail...MissingMethodException
				// TODO: be smarter and try to find a ctor with a string, else an empty ctor?

				try
				{
					var ct1 = t.GetConstructor(new Type[] { typeof(string) });
					if (ct1 != null)
						newE = (Exception)ct1.Invoke(new object[] { e.Message! });
//					Activator.CreateInstance(t, e.Mess, );
				}
				catch (MissingMethodException)
				{
				}

				if (newE == null)
				{
					try
					{
						var ct1 = t.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
						if (ct1 != null)
							newE = (Exception)ct1.Invoke(new object[] { e.Message!, null! });
						//newE = (Exception)Activator.CreateInstance(t, e.Mess, null);
					}
					catch (MissingMethodException)
					{
					}
				}

				if (newE == null)
				{
					try
					{
						var ct1 = t.GetConstructor(new Type[] { });
						if (ct1 != null)
							newE = (Exception)ct1.Invoke(new object[] { });
						// empty ctor
						//newE = (Exception)Activator.CreateInstance(t);
					}
					catch (MissingMethodException)
					{
					}
				}
			}

			if (newE == null)			
			{
				newE = new TypelessException(e.Message!);
			}

			// set stack
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(newE, e.StackTrace + System.Environment.NewLine);

			return newE;
		}

		public string Name => "MemoryPack";
	}

	public class TypelessException : Exception
	{
        public TypelessException(string mess) : base(mess)
        {
            
        }
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

				

		public ExceptionWrapper(Exception ex2)
		{
			TypeName = TypeShortener.GetShortType(ex2.GetType());
			Message = ex2.Message;

			StackTrace = ex2.StackTrace;
		//	FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
	//		remoteStackTraceString.SetValue(ex2, ex2.StackTrace + System.Environment.NewLine);
//			 St
		}
	}

	[MemoryPackable]
	partial class MemPackObjectArray
	{
		[UnsafeObjectArrayFormatter]
		public object[] Datas { get; set; } = null!;
	}
}
