using System.Text;
using MessagePack;
using MessagePack.Formatters;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		/// <summary>
		/// v2: type no longer written
		/// </summary>
		public string Name => "MessagePack_v2";

		public ExceptionStrategy ExceptionStrategy { get; set; } = ExceptionStrategy.UninitializedObject;

		public MessagePackSerializerOptions? Options { get; set; } = null;

		public void Serialize(Stream stream, object?[] graph, Type[] types)
		{
			//			Generator();

			//Datas[] typeAndObjects = new Datas[graph.Length];
			//for (int i = 0; i < graph.Length; i++)
			//{
			//	var obj = graph[i];
			//	typeAndObjects[i] = new Datas { Data = obj };
			//}

			var t = GetArgsType(types);
			var args = (IArgs)Activator.CreateInstance(t);
			args.Set(graph);
			object o = args;

			MessagePackSerializer.Serialize(t, stream, o, Options);
		}

		public object?[] Deserialize(Stream stream, Type[] types)
		{
			var t = GetArgsType(types);

			var res = (IArgs)MessagePackSerializer.Deserialize(t, stream, Options)!;
			return res.Get();

			//object?[] res = new object[types.Length];

			//using var r = new MessagePackStreamReader(stream, true);

			//var ae = r.ReadArrayAsync(CancellationToken.None).GetAsyncEnumerator();
			//try
			//{
			//	int i = 0;
			//	while (ae.MoveNextAsync().GetAwaiter().GetResult())
			//	{
			//		var c = ae.Current;

			//		// problem: if we supported references, we would have lost references in the other arguments (since we desser every arg. individually but serialize together)
			//		// So...
			//		res[i] = MessagePackSerializer.Deserialize(types[i], c, Options);
			//		i++;
			//	}
			//}
			//finally
			//{
			//	ae.DisposeAsync().GetAwaiter().GetResult();
			//}

			////var typeAndObjects = MessagePackSerializer.Deserialize<Datas[]>(stream, Options)!;
			////object?[] res = new object?[typeAndObjects.Length];
			////for (int i = 0; i < typeAndObjects.Length; i++)
			////{
			////	var to = typeAndObjects[i];
			////	res[i] = to.Data;
			////}
			////return res;
			//return res;
		}

		//[MessagePackObject]
		//public class Datas
		//{
		//	[Key(0)]
		//	[MessagePackFormatter(typeof(TypelessFormatter))]
		//	public object? Data { get; set; }
		//}

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
			}; ;
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

		public Type ExceptionType => typeof(ExceptionWrapper);


		[MessagePackObject(true)]
		public class ExceptionWrapper
		{
			
			public string TypeName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
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

			sb.AppendLine("[MessagePackObject]");

			string clas = "public class Args<";

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
			{
				sb.AppendLine($"[Key({i - 1})]");
				sb.AppendLine($"public T{i}? Arg{i} {{ get; set; }}");
			}

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

	[MessagePackObject]
	public class Args<T1> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		public object?[] Get() => new object?[] { Arg1 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
		}
	}

	[MessagePackObject]
	public class Args<T1, T2> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
			Arg2 = (T2?)args[1];
		}
	}

	[MessagePackObject]
	public class Args<T1, T2, T3> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
			Arg2 = (T2?)args[1];
			Arg3 = (T3?)args[2];
		}
	}

	[MessagePackObject]
	public class Args<T1, T2, T3, T4> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
		public T7? Arg7 { get; set; }
		[Key(7)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
		public T7? Arg7 { get; set; }
		[Key(7)]
		public T8? Arg8 { get; set; }
		[Key(8)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
		public T7? Arg7 { get; set; }
		[Key(7)]
		public T8? Arg8 { get; set; }
		[Key(8)]
		public T9? Arg9 { get; set; }
		[Key(9)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
		public T7? Arg7 { get; set; }
		[Key(7)]
		public T8? Arg8 { get; set; }
		[Key(8)]
		public T9? Arg9 { get; set; }
		[Key(9)]
		public T10? Arg10 { get; set; }
		[Key(10)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IArgs
	{
		[Key(0)]
		public T1? Arg1 { get; set; }
		[Key(1)]
		public T2? Arg2 { get; set; }
		[Key(2)]
		public T3? Arg3 { get; set; }
		[Key(3)]
		public T4? Arg4 { get; set; }
		[Key(4)]
		public T5? Arg5 { get; set; }
		[Key(5)]
		public T6? Arg6 { get; set; }
		[Key(6)]
		public T7? Arg7 { get; set; }
		[Key(7)]
		public T8? Arg8 { get; set; }
		[Key(8)]
		public T9? Arg9 { get; set; }
		[Key(9)]
		public T10? Arg10 { get; set; }
		[Key(10)]
		public T11? Arg11 { get; set; }
		[Key(11)]
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
