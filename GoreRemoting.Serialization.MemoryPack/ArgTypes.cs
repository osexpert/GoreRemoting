using System;
using System.Collections.Generic;
using System.Text;
using MemoryPack;

namespace GoreRemoting.Serialization.MemoryPack.ArgTypes
{
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

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IArgs
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
		public T13? Arg13 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13 };
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
			Arg13 = (T13?)args[12];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public T16? Arg16 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15, Arg16 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
			Arg16 = (T16?)args[15];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public T16? Arg16 { get; set; }
		public T17? Arg17 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15, Arg16, Arg17 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
			Arg16 = (T16?)args[15];
			Arg17 = (T17?)args[16];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public T16? Arg16 { get; set; }
		public T17? Arg17 { get; set; }
		public T18? Arg18 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15, Arg16, Arg17, Arg18 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
			Arg16 = (T16?)args[15];
			Arg17 = (T17?)args[16];
			Arg18 = (T18?)args[17];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public T16? Arg16 { get; set; }
		public T17? Arg17 { get; set; }
		public T18? Arg18 { get; set; }
		public T19? Arg19 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15, Arg16, Arg17, Arg18, Arg19 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
			Arg16 = (T16?)args[15];
			Arg17 = (T17?)args[16];
			Arg18 = (T18?)args[17];
			Arg19 = (T19?)args[18];
		}
	}

	[MemoryPackable]
	partial class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> : IArgs
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
		public T13? Arg13 { get; set; }
		public T14? Arg14 { get; set; }
		public T15? Arg15 { get; set; }
		public T16? Arg16 { get; set; }
		public T17? Arg17 { get; set; }
		public T18? Arg18 { get; set; }
		public T19? Arg19 { get; set; }
		public T20? Arg20 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8, Arg9, Arg10, Arg11, Arg12, Arg13, Arg14, Arg15, Arg16, Arg17, Arg18, Arg19, Arg20 };
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
			Arg13 = (T13?)args[12];
			Arg14 = (T14?)args[13];
			Arg15 = (T15?)args[14];
			Arg16 = (T16?)args[15];
			Arg17 = (T17?)args[16];
			Arg18 = (T18?)args[17];
			Arg19 = (T19?)args[18];
			Arg20 = (T20?)args[19];
		}
	}


}
