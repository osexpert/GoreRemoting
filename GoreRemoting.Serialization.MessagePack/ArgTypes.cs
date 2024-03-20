using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace GoreRemoting.Serialization.MessagePack.ArgTypes
{
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IArgs
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
		[Key(12)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
		public T15? Arg15 { get; set; }
		[Key(15)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
		public T15? Arg15 { get; set; }
		[Key(15)]
		public T16? Arg16 { get; set; }
		[Key(16)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
		public T15? Arg15 { get; set; }
		[Key(15)]
		public T16? Arg16 { get; set; }
		[Key(16)]
		public T17? Arg17 { get; set; }
		[Key(17)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
		public T15? Arg15 { get; set; }
		[Key(15)]
		public T16? Arg16 { get; set; }
		[Key(16)]
		public T17? Arg17 { get; set; }
		[Key(17)]
		public T18? Arg18 { get; set; }
		[Key(18)]
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

	[MessagePackObject]
	public class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> : IArgs
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
		[Key(12)]
		public T13? Arg13 { get; set; }
		[Key(13)]
		public T14? Arg14 { get; set; }
		[Key(14)]
		public T15? Arg15 { get; set; }
		[Key(15)]
		public T16? Arg16 { get; set; }
		[Key(16)]
		public T17? Arg17 { get; set; }
		[Key(17)]
		public T18? Arg18 { get; set; }
		[Key(18)]
		public T19? Arg19 { get; set; }
		[Key(19)]
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
