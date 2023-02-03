using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GrpcRemoting.Serialization;

namespace GrpcRemoting.RpcMessaging
{
	/// <summary>
	/// Method call message builder component.
	/// </summary>
	public class MethodCallMessageBuilder //: IMethodCallMessageBuilder
	{
		/// <summary>
		/// Builds a new method call message.
		/// </summary>
		/// <param name="serializer">Serializer adapter used to serialize argument values</param>
		/// <param name="remoteServiceName">Unique name of the remote service that should be called</param>
		/// <param name="targetMethod">Target method information</param>
		/// <param name="args">Array of arguments, which should passed a parameters</param>
		/// <returns>The created method call message</returns>
		public MethodCallMessage BuildMethodCallMessage(
			ISerializerAdapter serializer,
			string remoteServiceName,
			MethodInfo targetMethod,
			object[] args)
		{
			if (targetMethod == null)
				throw new ArgumentNullException(nameof(targetMethod));

			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			args ??= new object[0];

			var genericArgumentTypeNames =
				targetMethod.GetGenericArguments()
					.Select(arg => arg.FullName + "," + arg.Assembly.GetName().Name)
					.ToArray();

			var message = new MethodCallMessage()
			{
				ServiceName = remoteServiceName,
				MethodName = targetMethod.Name,
				Parameters = BuildMethodParameterInfos(serializer, targetMethod, args).ToArray(),
				GenericArgumentTypeNames = genericArgumentTypeNames,
				//CallContextSnapshot = CallContext.GetSnapshot()
			};

			return message;
		}

		/// <summary>
		/// Builds method call parameter messages from arguments for a specified target method.
		/// </summary>
		/// <param name="serializer">Serializer adapter used to serialize argument values</param>
		/// <param name="targetMethod">Target method information</param>
		/// <param name="args">Array of arguments, which should passed a parameters</param>
		/// <returns>Enumerable of method call parameter messages</returns>
		public IEnumerable<MethodCallParameterMessage> BuildMethodParameterInfos(
			ISerializerAdapter serializer,
			MethodInfo targetMethod,
			object[] args)
		{
			var parameterInfos = targetMethod.GetParameters();

			for (var i = 0; i < parameterInfos.Length; i++)
			{
				var arg = args[i];
				var parameterInfo = parameterInfos[i];

				if (parameterInfo.IsRefParameterForReal())
					throw new NotSupportedException("ref parameter not supported");

				var useParamArray =
					args.Length > parameterInfos.Length &&
					i == parameterInfos.Length - 1 &&
					parameterInfos[i].GetCustomAttribute<ParamArrayAttribute>() != null;

				var paramArrayValues = new List<object>();

				if (useParamArray)
				{
					for (var j = i; j < args.Length; j++)
					{
						paramArrayValues.Add(args[j]);
					}
				}

				object parameterValue =	useParamArray ? paramArrayValues.ToArray() : arg;

				yield return
					new MethodCallParameterMessage()
					{
						ParameterName = parameterInfo.Name,
						ParameterTypeName = parameterInfo.ParameterType.FullName + "," + parameterInfo.ParameterType.Assembly.GetName().Name,
						Value = parameterValue
					};
			}
		}

		/// <summary>
		/// Builds a new method call result message.
		/// </summary>
		/// <param name="serializer">Serializer adapter used to serialize argument values</param>
		/// <param name="method">Method information of the called method</param>
		/// <param name="args">Arguments</param>
		/// <param name="returnValue">Returned return value</param>
		/// <returns>Method call result message</returns>
		public MethodCallResultMessage BuildMethodCallResultMessage(
			ISerializerAdapter serializer,
			MethodInfo method,
			object[] args,
			object returnValue)
		{
			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			var parameterInfos = method.GetParameters();

			var message = new MethodCallResultMessage()
			{
				ReturnValue = returnValue
			};

			var outParameters = new List<MethodCallOutParameterMessage>();

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				var parameterInfo = parameterInfos[i];

				if (parameterInfo.IsOutParameterForReal())
				{
					outParameters.Add(
						new MethodCallOutParameterMessage()
						{
							ParameterName = parameterInfo.Name,
							OutValue = arg
						});
				}
			}

			message.OutParameters = outParameters.ToArray();
			//message.CallContextSnapshot = CallContext.GetSnapshot();

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
