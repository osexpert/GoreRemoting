using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace GoreRemoting.Serialization.Protobuf.ArgTypes
{
	interface IArgs
	{
		object?[] Get();
		void Set(object?[] args);
	}

	[ProtoContract]
	class Args<T1> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		public object?[] Get() => new object?[] { Arg1 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
		}
	}

	[ProtoContract]
	class Args<T1, T2> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
			Arg2 = (T2?)args[1];
		}
	}

	[ProtoContract]
	class Args<T1, T2, T3> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		public object?[] Get() => new object?[] { Arg1, Arg2, Arg3 };
		public void Set(object?[] args)
		{
			Arg1 = (T1?)args[0];
			Arg2 = (T2?)args[1];
			Arg3 = (T3?)args[2];
		}
	}

	[ProtoContract]
	class Args<T1, T2, T3, T4> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
		public T15? Arg15 { get; set; }
		[ProtoMember(16)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
		public T15? Arg15 { get; set; }
		[ProtoMember(16)]
		public T16? Arg16 { get; set; }
		[ProtoMember(17)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
		public T15? Arg15 { get; set; }
		[ProtoMember(16)]
		public T16? Arg16 { get; set; }
		[ProtoMember(17)]
		public T17? Arg17 { get; set; }
		[ProtoMember(18)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
		public T15? Arg15 { get; set; }
		[ProtoMember(16)]
		public T16? Arg16 { get; set; }
		[ProtoMember(17)]
		public T17? Arg17 { get; set; }
		[ProtoMember(18)]
		public T18? Arg18 { get; set; }
		[ProtoMember(19)]
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

	[ProtoContract]
	class Args<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> : IArgs
	{
		[ProtoMember(1)]
		public T1? Arg1 { get; set; }
		[ProtoMember(2)]
		public T2? Arg2 { get; set; }
		[ProtoMember(3)]
		public T3? Arg3 { get; set; }
		[ProtoMember(4)]
		public T4? Arg4 { get; set; }
		[ProtoMember(5)]
		public T5? Arg5 { get; set; }
		[ProtoMember(6)]
		public T6? Arg6 { get; set; }
		[ProtoMember(7)]
		public T7? Arg7 { get; set; }
		[ProtoMember(8)]
		public T8? Arg8 { get; set; }
		[ProtoMember(9)]
		public T9? Arg9 { get; set; }
		[ProtoMember(10)]
		public T10? Arg10 { get; set; }
		[ProtoMember(11)]
		public T11? Arg11 { get; set; }
		[ProtoMember(12)]
		public T12? Arg12 { get; set; }
		[ProtoMember(13)]
		public T13? Arg13 { get; set; }
		[ProtoMember(14)]
		public T14? Arg14 { get; set; }
		[ProtoMember(15)]
		public T15? Arg15 { get; set; }
		[ProtoMember(16)]
		public T16? Arg16 { get; set; }
		[ProtoMember(17)]
		public T17? Arg17 { get; set; }
		[ProtoMember(18)]
		public T18? Arg18 { get; set; }
		[ProtoMember(19)]
		public T19? Arg19 { get; set; }
		[ProtoMember(20)]
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
