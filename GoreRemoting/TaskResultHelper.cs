using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GoreRemoting
{
	internal static class TaskResultHelper
	{
		public static async Task<object?> GetTaskResult(MethodInfo method, object resultIn)
		{
			object? resultOut = resultIn;

			if (resultIn != null)
			{
				var returnType = method.ReturnType;

				if (returnType == typeof(Task))
				{
					var resultTask = (Task)resultIn;
					await resultTask.ConfigureAwait(false);
					resultOut = null;
				}
				else if (returnType == typeof(ValueTask))
				{
					var resultTask = (ValueTask)resultIn;
					await resultTask.ConfigureAwait(false);
					resultOut = null;
				}
				else if (returnType.IsGenericType)
				{
					if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
					{
						var resultTask = (Task)resultIn;
						await resultTask.ConfigureAwait(false);

						resultOut = returnType.GetProperty("Result")?.GetValue(resultIn); // why '?' ?
					}
					else if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
					{
						//await ToTask((dynamic)result).ConfigureAwait(false);

						//var resultTask = (dynamic)result;
						//await resultTask.ConfigureAwait(false);

						var valueTaskToTask = typeof(TaskResultHelper)
							.GetMethod(nameof(ValueTaskToTask), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
							.MakeGenericMethod(returnType.GenericTypeArguments.Single());
						// OR GetGenericArguments ?

						await (Task)valueTaskToTask.Invoke(null, new[] { resultIn });

						resultOut = returnType.GetProperty("Result")?.GetValue(resultIn); // why '?' ?
					}
				}
			}

			return resultOut;
		}

		//private static Task<T> ToTask<T>(ValueTask<T> task)
		//{
		//	return task.AsTask();
		//}

		private static Task ValueTaskToTask<T>(ValueTask<T> task)
		{
			return task.AsTask();
		}

	}
}
