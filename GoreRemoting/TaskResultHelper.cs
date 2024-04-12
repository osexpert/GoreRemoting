using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GoreRemoting
{
	internal static class TaskResultHelper
	{
		public static async Task<object?> GetTaskResult(MethodInfo method, object? resultIn)
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
						resultOut = returnType.GetProperty("Result").GetValue(resultIn);
					}
					else if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
					{
						var resultTask = (Task)returnType.GetMethod("AsTask").Invoke(resultIn, new object[] { });
						await resultTask.ConfigureAwait(false);
						resultOut = returnType.GetProperty("Result").GetValue(resultIn);
					}
				}
			}

			return resultOut;
		}
	}
}
