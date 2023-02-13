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
		public static IAsyncEnumerable<T> Consume<T>(Func<Func<T, Task>, Task> dataSource)
		{
			Channel<T> channel = Channel.CreateUnbounded<T>();

			Task.Run(async () =>
			{
				try
				{
					await dataSource(data => channel.Writer.WriteAsync(data).AsTask()).ConfigureAwait(false);
					channel.Writer.Complete();
				}
				catch (Exception e)
				{
					channel.Writer.Complete(e);
				}
			});

			return channel.Reader.ReadAllAsync();
		}

		public static async Task Produce<T>(Func<IAsyncEnumerable<T>> source, Func<T, Task> target)
		{
			await foreach (var a in source().ConfigureAwait(false))
				await target(a).ConfigureAwait(false);
		}

	}

#endif
}
