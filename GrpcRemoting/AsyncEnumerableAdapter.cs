using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GrpcRemoting
{

#if NETSTANDARD2_1
	public static class AsyncEnumerableAdapter
	{
		public static IAsyncEnumerable<T> Consume<T>(Func<Func<T, Task>, Task> dataSource, CancellationToken cancel = default)
		{
			Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = true
			});

			_ = Task.Run(async () =>
			{
				try
				{
					await dataSource(data => channel.Writer.WriteAsync(data, cancel).AsTask()).ConfigureAwait(false);
					channel.Writer.Complete();
				}
				catch (Exception e)
				{
					channel.Writer.Complete(e);
				}
			});

			return channel.Reader.ReadAllAsync(cancel);
		}

		public static async Task Produce<T>(Func<IAsyncEnumerable<T>> source, Func<T, Task> target)
		{
			await foreach (var a in source().ConfigureAwait(false))
				await target(a).ConfigureAwait(false);
		}
		public static async Task Produce<T>(Func<CancellationToken, IAsyncEnumerable<T>> source, Func<T, Task> target, CancellationToken cancel = default)
		{
			await foreach (var a in source(cancel).ConfigureAwait(false))
				await target(a).ConfigureAwait(false);
		}
	}

#endif
}
