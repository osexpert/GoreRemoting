using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoreRemoting.RpcMessaging
{
	/// <summary>
	/// Method call message builder component.
	/// </summary>
	public class MethodCallMessageBuilder //: IMethodCallMessageBuilder
	{
		/// <summary>
		/// Builds a new method call message.
		/// </summary>
		/// <param name="targetMethod">Target method information</param>
		/// <param name="args">Array of arguments, which should passed a parameters</param>
		/// <returns>The created method call message</returns>
		public MethodCallMessage BuildMethodCallMessage(
			MethodInfo targetMethod,
			object?[] args,
			bool emitCallContext)
		{
			if (targetMethod == null)
				throw new ArgumentNullException(nameof(targetMethod));

			//args ??= new object[0];

			var message = new MethodCallMessage()
			{
				Arguments = BuildMethodParameterInfos(
					targetMethod,
					 args
					).ToArray(),
			};

			if (emitCallContext)
				message.CallContextSnapshot = CallContext.GetSnapshot();

			return message;
		}

		/// <summary>
		/// Builds method call parameter messages from arguments for a specified target method.
		/// </summary>
		/// <param name="targetMethod">Target method information</param>
		/// <param name="args">Array of arguments, which should passed a parameters</param>
		/// <returns>Enumerable of method call parameter messages</returns>
		public IEnumerable<MethodCallArgument> BuildMethodParameterInfos(
			MethodInfo targetMethod,
			object?[] args
			)
		{
			var parameterInfos = targetMethod.GetParameters();

			// TODO: throw if more args than params?
			if (args.Length != parameterInfos.Length)
				throw new Exception("args vs params count mismatch");

			for (var i = 0; i < parameterInfos.Length; i++)
			{
				var arg = args[i];
				var parameterInfo = parameterInfos[i];

				if (parameterInfo.IsRefParameterForReal())
					throw new NotSupportedException("ref parameter not supported");

				//var useParamArray =
				//	args.Length > parameterInfos.Length && // more args than params? not possible...unless...BSON?
				//	i == parameterInfos.Length - 1 &&
				//	parameterInfos[i].GetCustomAttribute<ParamArrayAttribute>() != null;

				//var paramArrayValues = new List<object>();

				//if (useParamArray)
				//{
				//	// will never happen for binary formatter?
				//	for (var j = i; j < args.Length; j++)
				//	{
				//		paramArrayValues.Add(args[j]);
				//	}
				//}

				//object parameterValue =	useParamArray ? paramArrayValues.ToArray() : arg;

				yield return
					new MethodCallArgument()
					{
						Position = i,
						ParameterName = parameterInfo.Name,
						Value = arg,
						IsOut = parameterInfo.IsOutParameterForReal()
					};
			}
		}

		/// <summary>
		/// Builds a new method call result message.
		/// </summary>
		/// <param name="method">Method information of the called method</param>
		/// <param name="args">Arguments</param>
		/// <param name="returnValue">Returned return value</param>
		/// <returns>Method call result message</returns>
		public MethodResultMessage BuildMethodCallResultMessage(
			MethodInfo method,
			object?[] args,
			object? returnValue,
			bool emitCallContext)
		{
			var parameterInfos = method.GetParameters();

			bool voidReturn = 
				method.ReturnType == typeof(void)
				|| method.ReturnType == typeof(Task) 
				|| method.ReturnType == typeof(ValueTask);

			var message = new MethodResultMessage()
			{
				// fixme: ctor
				Value = returnValue,
				ResultType = voidReturn ? ResultKind.ResultVoid : ResultKind.ResultValue
			};

			var outArguments = new List<MethodOutArgument>();

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				var parameterInfo = parameterInfos[i];

				if (parameterInfo.IsOutParameterForReal())
				{
					outArguments.Add(
						new MethodOutArgument()
						{
							ParameterName = parameterInfo.Name,
							Position = i,
							OutValue = arg // NOT
						});
				}
			}

			message.OutArguments = outArguments.ToArray();

			if (emitCallContext)
				message.CallContextSnapshot = CallContext.GetSnapshot();

			return message;
		}

	}

	public static class ParameterInfoExtensions
	{
		/// <summary>
		/// https://stackoverflow.com/a/38110036/2671330
		/// </summary>
		/// <param name="pi"></param>
		/// <returns></returns>
		public static bool IsRefParameterForReal(this ParameterInfo pi) => pi.ParameterType.IsByRef && !pi.IsOut;

		/// <summary>
		/// https://stackoverflow.com/a/38110036/2671330
		/// </summary>
		/// <param name="pi"></param>
		/// <returns></returns>
		public static bool IsOutParameterForReal(this ParameterInfo pi) => pi.ParameterType.IsByRef && pi.IsOut;
	}
}
