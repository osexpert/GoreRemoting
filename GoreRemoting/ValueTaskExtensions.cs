using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting
{
	public static class ValueTaskExtensions
	{
		public static void GetResult(this ValueTask valueTask)
		{
			if (valueTask.IsCompleted)
			{
				if (valueTask.IsCompletedSuccessfully)
				{
					// noop
				}
				else
				{
					valueTask.GetAwaiter().GetResult();
				}
			}
			else
			{
				valueTask.AsTask().GetAwaiter().GetResult();
			}
		}

		public static T GetResult<T>(this ValueTask<T> valueTask)
		{
			if (valueTask.IsCompleted)
			{
				if (valueTask.IsCompletedSuccessfully)
				{
					return valueTask.Result;
				}
				else
				{
					return valueTask.GetAwaiter().GetResult();
				}
			}
			else
			{
				return valueTask.AsTask().GetAwaiter().GetResult();
			}
		}

		public static void Discard(this ValueTask valueTask)
		{
			if (valueTask.IsCompleted)
			{
				if (valueTask.IsCompletedSuccessfully)
				{
					// noop
				}
				else
				{
					valueTask.GetAwaiter().GetResult();
				}
			}
			else
			{
				_ = valueTask.AsTask();
			}
		}

		public static void Discard<T>(this ValueTask<T> valueTask)
		{
			if (valueTask.IsCompleted)
			{
				if (valueTask.IsCompletedSuccessfully)
				{
					// noop
				}
				else
				{
					_ = valueTask.GetAwaiter().GetResult();
				}
			}
			else
			{
				_ = valueTask.AsTask();
			}
		}
	}
}
