using System;
using System.Text;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack
{
	public class MemoryPackAdapter : ISerializerAdapter
	{
		public MemoryPackSerializerOptions? Options { get; set; }

		public ExceptionStrategy ExceptionStrategy { get; set; } = ExceptionStrategy.UninitializedObject;

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			//			Generator();

			var t = GetArgsType(types);
			var args = (IArgs)Activator.CreateInstance(t);
			args.Set(graph);
			object o = args;

			//MemoryPackSerializer.SerializeAsync<MemPackObjectArray>(stream, new MemPackObjectArray { Datas = graph }, Options).GetAwaiter().GetResult();
			MemoryPackSerializer.SerializeAsync(t, stream, o, Options).GetAwaiter().GetResult();
		}

//		internal static AsyncLocal<Type[]?> _asyncLocalTypes = new AsyncLocal<Type[]?>();

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <returns>Deserialized object graph</returns>
		public object?[] Deserialize(Stream stream, Type[] types)
		{
			try
			{
				//if (_asyncLocalTypes.Value != null)
				//	throw new Exception("types already set");
				//_asyncLocalTypes.Value = types;

				//			long pos = stream.Position;

				var t = GetArgsType(types);
				//args

				//var res = MemoryPackSerializer.DeserializeAsync<MemPackObjectArray>(stream, Options).GetAwaiter().GetResult()!.Datas;
				var res = (IArgs)MemoryPackSerializer.DeserializeAsync(t, stream, Options).GetAwaiter().GetResult()!;
				return res.Get();
			}
			finally
			{
			//	_asyncLocalTypes.Value = null;
			}
		}

		public object GetSerializableException(Exception ex)
		{
			return ToExceptionWrapper(ExceptionSerializationHelpers.GetExceptionData(ex));
		}

		public Exception RestoreSerializedException(object ex)
		{
			var ew = (ExceptionWrapper)ex;
			return ExceptionStrategy switch
			{
				// TODO: add exception allow list, use Type.ToString() as lookup
				ExceptionStrategy.UninitializedObject => ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew), Type.GetType(ew.TypeName)),
				ExceptionStrategy.RemoteInvocationException => ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(ToExceptionData(ew)),
				_ => throw new NotSupportedException("ExceptionStrategy: " + ExceptionStrategy)
			};
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


		/// <summary>
		/// v2: type no longer written.
		/// </summary>
		public string Name => "MemoryPack_v2";

		public Type ExceptionType => typeof(ExceptionWrapper);


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

			sb.AppendLine("[MemoryPackable]");
			
			string clas = "partial class Args<";

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
				sb.AppendLine($"Arg{i} = (T{i}?)args[{i - 1}];");
			sb.AppendLine("}");

			sb.AppendLine("}");

			sb.AppendLine();
			return sb.ToString();
		}
	}

	//[MemoryPackable]
	//public partial class Arg<T1>
	//{
	//}

	[MemoryPackable]
	partial class ExceptionWrapper
	{
		public string TypeName { get; set; }
		public Dictionary<string, string> PropertyData { get; set; }
	}

	//[MemoryPackable]
	//partial class MemPackObjectArray
	//{
	//	[ObjectArrayFormatter]
	//	public object?[] Datas { get; set; } = null!;
	//}

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

	interface IArgs
	{
		object?[] Get();
		void Set(object?[] args);
	}

	[MemoryPackable]
	partial class Args<T1> : IArgs
	{
		public T1? Arg1 { get; set; }
		public object?[] Get() => new object?[] { Arg1 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IArgs
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IArgs
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


}
