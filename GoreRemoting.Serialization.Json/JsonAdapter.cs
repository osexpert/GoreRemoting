using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TupleAsJsonArray;

namespace GoreRemoting.Serialization.Json
{

	public class JsonAdapter : ISerializerAdapter
	{
		public JsonSerializerOptions Options { get; }

		internal static AsyncLocal<Type[]?> _asyncLocalTypes = new AsyncLocal<Type[]?>();

		public ExceptionStrategy ExceptionStrategy { get; set; } = ExceptionStrategy.UninitializedObject;

		public JsonAdapter()
		{
			Options = CreateOptions();
		}

		private static JsonSerializerOptions CreateOptions()
		{
			return new JsonSerializerOptions()
			{
				IncludeFields = true,
				ReferenceHandler = ReferenceHandler.Preserve,
				Converters =
				{
					new TupleConverterFactory()
				}
			};
		}

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
//			Generator();

			Dictionary<int, byte[]> byteArrays = new();
			object?[] objects = new object[graph.Length];

			for (int i = 0; i < graph.Length; i++)
			{
				var obj = graph[i];
				if (obj is byte[] bs)
				{
					byteArrays.Add(i, bs);
				}
				else
				{
					objects[i] = obj;
				}
			}

			var bw = new GoreBinaryWriter(stream);
			bw.WriteVarInt(byteArrays.Count);
			foreach (var byteArray in byteArrays)
			{
				bw.WriteVarInt(byteArray.Key);
				bw.WriteVarInt(byteArray.Value.Length);
				bw.Write(byteArray.Value);
			}

			Type t = GetArgsType(types);

			var ins = (IArgs)Activator.CreateInstance(t)!;

			ins.Set(objects);

			// very strange...but this is required!
			object o = ins;

			JsonSerializer.Serialize(stream, o, Options);

			//JsonSerializer.Serialize<ObjectOnly>(stream, new ObjectOnly(objects), Options);
		}

		private static Type GetArgsType(Type[] types)
		{
			var type = types.Length switch
			{
				1 => typeof(Args<>).MakeGenericType(types),
				2 => typeof(Args<,>).MakeGenericType(types),
				3 => typeof(Args<,,>).MakeGenericType(types),
				4 => typeof(Args<,,,>).MakeGenericType(types),
				5 => typeof(Args<,,,,>).MakeGenericType(types),
				6 => typeof(Args<,,,,,>).MakeGenericType(types),
				7 => typeof(Args<,,,,,,>).MakeGenericType(types),
				8 => typeof(Args<,,,,,,,>).MakeGenericType(types),
				9 => typeof(Args<,,,,,,,,>).MakeGenericType(types),
				10 => typeof(Args<,,,,,,,,,>).MakeGenericType(types),
				11 => typeof(Args<,,,,,,,,,,>).MakeGenericType(types),
				12 => typeof(Args<,,,,,,,,,,,>).MakeGenericType(types),
				_ => throw new NotImplementedException("Too many arguments")
			};
			return type;
		}

		//class ByteArray
		//{
		//	public int Idx;
		//	public byte[] Bytes;

		//	public ByteArray(int idx, byte[] bytes)
		//	{
		//		Idx = idx;
		//		Bytes = bytes;
		//	}
		//}



		//class ObjectOnly
		//{
		//	[JsonConverter(typeof(ObjectFormatter))]
		//	public object? Data { get; set; }

		//	public ObjectOnly(object? data)
		//	{
		//		Data = data;
		//	}
		//}

		interface IArgs
		{
			object?[] Get();
			void Set(object?[] args);
		}
		class Args<T1> : IArgs
		{
			public T1? Arg1 { get; set; }
			public object?[] Get() => new object?[] { Arg1 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
			}
		}

		class Args<T1, T2> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
			}
		}

		class Args<T1, T2, T3> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
			}
		}

		class Args<T1, T2, T3, T4> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
			}
		}

		class Args<T1, T2, T3, T4, T5> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7, T8> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public T8? Arg8 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
				Arg8 = (T8?)args[7];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public T8? Arg8 { get; set; }
			public T9? Arg9 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
				Arg8 = (T8?)args[7];
				Arg9 = (T9?)args[8];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public T8? Arg8 { get; set; }
			public T9? Arg9 { get; set; }
			public T10? Arg10 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
				Arg8 = (T8?)args[7];
				Arg9 = (T9?)args[8];
				Arg10 = (T10?)args[9];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public T8? Arg8 { get; set; }
			public T9? Arg9 { get; set; }
			public T10? Arg10 { get; set; }
			public T11? Arg11 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
				Arg8 = (T8?)args[7];
				Arg9 = (T9?)args[8];
				Arg10 = (T10?)args[9];
				Arg11 = (T11?)args[10];
			}
		}

		class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IArgs
		{
			public T1? Arg1 { get; set; }
			public T2? Arg2 { get; set; }
			public T3? Arg3 { get; set; }
			public T4? Arg4 { get; set; }
			public T5? Arg5 { get; set; }
			public T6? Arg6 { get; set; }
			public T7? Arg7 { get; set; }
			public T8? Arg8 { get; set; }
			public T9? Arg9 { get; set; }
			public T10? Arg10 { get; set; }
			public T11? Arg11 { get; set; }
			public T12? Arg12 { get; set; }
			public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12 };
			public void Set(object?[] args)
			{
				Arg1 = (T1?)args[0];
				Arg2 = (T2?)args[1];
				Arg3 = (T3?)args[2];
				Arg4 = (T4?)args[3];
				Arg5 = (T5?)args[4];
				Arg6 = (T6?)args[5];
				Arg7 = (T7?)args[6];
				Arg8 = (T8?)args[7];
				Arg9 = (T9?)args[8];
				Arg10 = (T10?)args[9];
				Arg11 = (T11?)args[10];
				Arg12 = (T12?)args[11];
			}
		}



		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <returns>Deserialized object graph</returns>
		public object?[] Deserialize(Stream stream, Type[] types)
		{
			if (types.Length == 0)
				throw new Exception();

			try
			{
				if (_asyncLocalTypes.Value != null)
					throw new Exception("types already set");
				_asyncLocalTypes.Value = types;

				Dictionary<int, byte[]> byteArrays = new();

				//object?[] res = new object[types.Length];

				var br = new GoreBinaryReader(stream);
				var bCnt = br.ReadVarInt();
				while (bCnt-- > 0)
				{
					var idx = br.ReadVarInt();
					var byteLen = br.ReadVarInt();
					var bytes = br.ReadBytes(byteLen);
					byteArrays.Add(idx, bytes);
					//res[idx] = bytes;
				}

				Type t = GetArgsType(types);				

				var objects = (IArgs)JsonSerializer.Deserialize(stream, t, Options)!;

				var res = objects.Get();

				foreach (var kv in byteArrays)
					res[kv.Key] = kv.Value;

				return res;
				//if (objects.Data.Length != types.Length)
				//	throw new Exception("mismatch objects vs types count");

				//return objects.Data;

				//// maybe we can convert to same type as parameters?
				////https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround
				////
			}
			finally
			{
				_asyncLocalTypes.Value = null;
			}
		}


		public Type ExceptionType => typeof(ExceptionWrapper);

		
		class ExceptionWrapper
		{
			public string TypeName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
		}

		public object GetSerializableException(Exception ex)
		{
			return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex));
		}

		public Exception RestoreSerializedException(object ex)
		{
			//var ew = (ExceptionWrapper)Deserialize(typeof(ExceptionWrapper), ex)!;
			var ew = (ExceptionWrapper)ex;
			switch (ExceptionStrategy)
			{
				case ExceptionStrategy.UninitializedObject:
					// TODO: add exception allow list, use Type.ToString() as lookup
					return ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew), Type.GetType(ew.TypeName));
				case ExceptionStrategy.RemoteInvocationException:
					return ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(ToExceptionData(ew));
				default:
					throw new NotSupportedException("ExceptionStrategy: " + ExceptionStrategy);
			}
		}

		private static ExceptionWrapper ToExceptionWrapper(ExceptionData ed)
		{
			return new ExceptionWrapper
			{
				TypeName = ed.TypeName,
				PropertyData = ed.PropertyData,
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

		//public object? Deserialize(Type type, object? value)
		//{
		// // This is nice....BUT it does not work with references. ObjectArrayFormatter does
		//	if (value is JsonElement je)
		//		return je.Deserialize(type, Options);
		//	else
		//		return value;
		//}

		/// <summary>
		/// v2: Type no longer written.
		/// </summary>
		public string Name => "Json_v2";

		private static void Generator()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 1; i <= 12; i++)
				sb.Append(Generator(i));

			var res = sb.ToString();
		}

		private static string Generator(int args)
		{
			StringBuilder sb = new StringBuilder();

			string clas = "class Args<";

			List<string> tees = new List<string>();
			List<string> argss = new List<string>();
			for (int i = 1; i <= args; i++)
			{
				tees.Add("T" + i);
				argss.Add("Arg" + i);
			}

			clas += string.Join(", ", tees);

			clas += "> : IArgs";
			sb.AppendLine(clas);
			sb.AppendLine("{");
			for (int i = 1; i <= args; i++)
				sb.AppendLine($"public T{i}? Arg{i} {{ get; set; }}");

			sb.AppendLine($"public object?[] Get() => new object?[] {{ {string.Join(", ", argss)} }};");

			sb.AppendLine("public void Set(object?[] args)");
			sb.AppendLine("{");
			for (int i = 1; i <= args; i++)
				sb.AppendLine($"Arg{i} = (T{i}?)args[{i-1}];");
			sb.AppendLine("}");

			sb.AppendLine("}");

			sb.AppendLine();
			return sb.ToString();
		}
	}

	public enum ExceptionStrategy
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



	
}
