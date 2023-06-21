// Copyright (c) 2020 stakx
// License available at https://github.com/stakx/DynamicProxy.AsyncInterceptor/blob/master/LICENSE.md.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace stakx.DynamicProxy
{
	public interface ISyncInvocation
	{
		MethodInfo Method { get; }
		object ReturnValue { get; set; }
		object[] Arguments { get; }
	}

	//public class Invocation2 : IInvocation2
	//{
	//	public MethodInfo Method => _i.Method;

	//	public object ReturnValue
	//	{
	//		get => _i.ReturnValue;
	//		set => _i.ReturnValue = value;
	//	}

	//	public object[] Arguments => _i.Arguments;


	//	Castle.DynamicProxy.IInvocation _i;

	//	public Invocation2(IInvocation i)
	//	{
	//		_i = i;
	//	}
	//}


	public class SyncInvocation : ISyncInvocation
	{
		public MethodInfo Method { get; set; }
		public object ReturnValue { get; set; }
		public object[] Arguments { get; set; }
		public SyncInvocation(MethodInfo met, object[] args)
		{
			Method = met;
			Arguments = args;
		}
	}


	public partial class AsyncInterceptor //: IInterceptor
	{
		public AsyncInterceptor(Action<ISyncInvocation> sync, Func<IAsyncInvocation, ValueTask> asyncc)
		{
			_sync = sync;
			_asyncc = asyncc;

		}

		Action<ISyncInvocation> _sync;
		Func<IAsyncInvocation, ValueTask> _asyncc;

		public void Intercept(ISyncInvocation invocation)
		{
			var returnType = invocation.Method.ReturnType;
			var builder = AsyncMethodBuilder.TryCreate(returnType);
			if (builder != null)
			{
				var asyncInvocation = new AsyncInvocation(invocation);
				var stateMachine = new AsyncStateMachine(asyncInvocation, builder, task: _asyncc(asyncInvocation));
				builder.Start(stateMachine);
				invocation.ReturnValue = builder.Task();
			}
			else
			{
				_sync(invocation);
			}
		}

		//protected abstract void Intercept(IInvocation invocation);

		//protected abstract ValueTask InterceptAsync(IAsyncInvocation invocation);
	}
}
