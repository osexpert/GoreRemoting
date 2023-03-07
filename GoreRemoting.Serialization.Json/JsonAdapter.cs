using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TupleAsJsonArray;

namespace GoreRemoting.Serialization.Json
{
	public class JsonAdapter : ISerializerAdapter
	{
		static JsonSerializerOptions opt = new JsonSerializerOptions()
		{
			IncludeFields = true,
			ReferenceHandler = ReferenceHandler.Preserve,
			Converters =
			{
				new TupleConverterFactory(),
			}
			
		};

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream s, object[] graph)
		{
			//var bw = new GoreBinaryWriter(s);
			//bw.Write7BitEncodedInt(graph.Length);

			//foreach (var v in graph)
			//{
			//	if (v == null)
			//		bw.Write(false);
			//	else
			//	{
			//		bw.Write(true);
			//		bw.Write(v.GetType().AssemblyQualifiedName);
			//		System.Text.Json.JsonSerializer.Serialize(s, v, opt);
			//	}
			//}

			TypeAndObject[] g2 = new TypeAndObject[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var o = graph[i];

				if (o != null)
				{
					var t = o.GetType();
					var tao = new TypeAndObject() { TypeName = t.AssemblyQualifiedName, Data = o };
					g2[i] = tao;
				}
				else
					g2[i] = null;
			}

			System.Text.Json.JsonSerializer.Serialize<TypeAndObject[]>(s, g2, opt);
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			//var br = new GoreBinaryReader(stream);

			//var n = br.Read7BitEncodedInt();

			//object[] res = new object[n];

			//for (int i=0; i < n;i++)
			//{
			//	if (br.ReadBoolean())
			//	{
			//		var t = Type.GetType(br.ReadString());
			//		res[i] = JsonSerializer.Deserialize(stream, t, opt);
			//	}
			//	else
			//		res[i] = null;
			//}

			//return res;
			var tao = JsonSerializer.Deserialize<TypeAndObject[]>(stream, opt)!;

			object[] res = new object[tao.Length];

			for (int i = 0; i < tao.Length; i++)
			{
				var to = tao[i];
				if (to != null)
				{
					var t = Type.GetType(to.TypeName);
					res[i] = ((System.Text.Json.JsonElement)to.Data).Deserialize(t, opt);
				}
				else
					res[i] = null;
			}

			return res;
			//// maybe we can convert to same type as parameters?
			////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
			//	throw new NotImplementedException();
		}

		public object GetSerializableException(Exception ex2)
		{
			//return ex2;
			return new ExceptionWrapper(ex2);// return ex2.GetType().IsSerializable ? ex2 : new RemoteInvocationException(ex2.Message);
		}

		class ExceptionWrapper //: Exception //seems impossible that MemPack can inherit exception?
		{
			public string? TypeName { get; set; }
			public string? Mess { get; set; }
			public string? Stack { get; set; }

			public ExceptionWrapper()
			{

			}

			public ExceptionWrapper(Exception ex2)
			{
				TypeName = ex2.GetType().AssemblyQualifiedName;// UnsafeObjectFormatter.GetShortType(ex2.GetType());
				Mess = ex2.Message;

				Stack = ex2.StackTrace;
				//	FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
				//		remoteStackTraceString.SetValue(ex2, ex2.StackTrace + System.Environment.NewLine);
				//			 St
			}
		}

		public Exception RestoreSerializedException(object ex2)
		{
			//var newE = (Exception)ex2;
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
						newE = (Exception)ct1.Invoke(new object[] { e.Mess! });
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
							newE = (Exception)ct1.Invoke(new object[] { e.Mess!, null! });
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
				newE = new TypelessException(e.Mess!);
			}

			//// set stack
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(newE, e.Stack);
			remoteStackTraceString.SetValue(newE, newE.StackTrace + System.Environment.NewLine);

			return newE;
		}

		public class TypelessException : Exception
		{
			public TypelessException(string mess) : base(mess)
			{

			}
		}

		public string Name => "Json";
	}

	class TypeAndObject
	{
		public string TypeName { get; set; }
		public object Data { get; set; }
	}
}
