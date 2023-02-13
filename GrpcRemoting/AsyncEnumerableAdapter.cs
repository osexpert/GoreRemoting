using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GrpcRemoting
{

#if NETSTANDARD2_1
	public static class AsyncEnumerableAdapter
	{

		//Func<Func<T, Task>, Task> _value;

		//public AsyncEnumerableAdapter(Func<Func<T, Task>, Task> i)
		//{
		//	_value = value				;
		//}



		//public IAsyncEnumerable<T> Consume()
		//{
		//	return Consume<T>(_value);
		//}

		//public Task Produce(T arg)
		//{
		//	throw new NotImplementedException();
		//}

		public static IAsyncEnumerable<T> Consume<T>(Func<Func<T, Task>, Task> i)
		{
			Channel<T> c = Channel.CreateUnbounded<T>();

			Task.Run(async () =>
			{
				try
				{
					await i((w) => c.Writer.WriteAsync(w).AsTask()).ConfigureAwait(false);
					c.Writer.Complete();
				}
				catch (Exception e)
				{
					c.Writer.Complete(e);
				}
			});


			return c.Reader.ReadAllAsync();
		}

		public static async Task Produce<T>(Func<IAsyncEnumerable<T>> jild3Int, Func<T, Task> outt)
		{
			await foreach (var a in jild3Int().ConfigureAwait(false))
				await outt(a).ConfigureAwait(false);
		}

		//public static IAsyncEnumerable<T> Consume4(Func<Func<T, Task>, Task> i)
		//{
		//	return Consume(i);
		//}
	}

#endif
}
