// Copyright (c) 2020 stakx
// License available at https://github.com/stakx/DynamicProxy.AsyncInterceptor/blob/master/LICENSE.md.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace stakx.DynamicProxy
{
	internal static class AsyncMethodBuilder
	{


		public static Builder? TryCreate(Type returnType)
		{
			var builderType = GetAsyncMethodBuilderType(returnType);
			if (builderType != null)
			{
				var createMethod = builderType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
				var builder = createMethod.Invoke(null, null);
				return new Builder(builder);
			}
			else
			{
				return null;
			}
		}

		private static Type? GetAsyncMethodBuilderType(Type returnType)
		{
			var asyncMethodBuilderAttribute = (AsyncMethodBuilderAttribute)Attribute.GetCustomAttribute(returnType, typeof(AsyncMethodBuilderAttribute), inherit: false);
			if (asyncMethodBuilderAttribute != null)
			{
				var builderType = asyncMethodBuilderAttribute.BuilderType;
				if (builderType.IsGenericTypeDefinition)
				{
					Debug.Assert(returnType.IsConstructedGenericType);
					return builderType.MakeGenericType(returnType.GetGenericArguments());
				}
				else
				{
					return builderType;
				}
			}
			else if (returnType == typeof(ValueTask))
			{
				return typeof(AsyncValueTaskMethodBuilder);
			}
			else if (returnType == typeof(Task))
			{
				return typeof(AsyncTaskMethodBuilder);
			}
			else if (returnType.IsGenericType)
			{
				var returnTypeDefinition = returnType.GetGenericTypeDefinition();
				if (returnTypeDefinition == typeof(ValueTask<>))
				{
					return typeof(AsyncValueTaskMethodBuilder<>).MakeGenericType(returnType.GetGenericArguments()[0]); // .Single?
				}
				else if (returnTypeDefinition == typeof(Task<>))
				{
					return typeof(AsyncTaskMethodBuilder<>).MakeGenericType(returnType.GetGenericArguments()[0]); // .Single?
				}
			}
			// NOTE: `AsyncVoidMethodBuilder` is intentionally excluded here because we want to end up in a synchronous
			// `Intercept` callback for non-awaitable methods.
			return null;
		}


	}

	public class Builder
	{
		object _builder;

		public Builder(object b)
		{
			_builder = b;
		}

		public void AwaitOnCompleted(object awaiter, object stateMachine)
		{
			var awaitOnCompletedMethod = _builder.GetType().GetMethod("AwaitOnCompleted", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(awaiter.GetType(), stateMachine.GetType());
			awaitOnCompletedMethod.Invoke(_builder, new object[] { awaiter, stateMachine });
		}

		public void SetException(Exception exception)
		{
			var setExceptionMethod = _builder.GetType().GetMethod("SetException", BindingFlags.Public | BindingFlags.Instance);
			setExceptionMethod.Invoke(_builder, new object[] { exception });
		}

		public void SetResult(object? result)
		{
			var setResultMethod = _builder.GetType().GetMethod("SetResult", BindingFlags.Public | BindingFlags.Instance);
			if (setResultMethod.GetParameters().Length == 0)
			{
				setResultMethod.Invoke(_builder, null);
			}
			else
			{
				setResultMethod.Invoke(_builder, new object?[] { result });
			}
		}

		public void Start(object stateMachine)
		{
			var startMethod = _builder.GetType().GetMethod("Start", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(stateMachine.GetType());
			startMethod.Invoke(_builder, new object[] { stateMachine });
		}

		public object Task()
		{
			var taskProperty = _builder.GetType().GetProperty("Task", BindingFlags.Public | BindingFlags.Instance);
			return taskProperty.GetValue(_builder);
		}
	}
}
