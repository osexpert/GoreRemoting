using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GoreRemoting
{
	public static class ProgressAdapter
	{
		public static Action<T> ClientConsume<T>(IProgress<T> p)
		{
			return x => p.Report(x);
		}

		public static IProgress<T> ServerProduce<T>(Action<T> report)
		{
			return new IProgressWrapper<T>(report);
		}

		class IProgressWrapper<T> : IProgress<T>
		{
			Action<T> _report;

			public IProgressWrapper(Action<T> report)
			{
				_report = report;
			}

			public void Report(T value)
			{
				_report(value);
			}
		}
	}


#if NETSTANDARD2_1
	public static class AsyncEnumerableAdapter
	{
		public static IAsyncEnumerable<T> ClientConsume<T>(Func<Func<T, Task>, Task> dataSource, CancellationToken cancel = default)
		{
			Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = true
			});

			async Task Consume()
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
			}

			_ = Consume(); // fire and forget

			return channel.Reader.ReadAllAsync(cancel);
		}

		public static async Task ClientProduce<T>(IAsyncEnumerable<T> source, Func<Func<Task<(T, bool)>>, Task> dataProvider)
		{
			Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = true
			});

			async Task Provide()
			{
				await dataProvider(async () => (await channel.Reader.ReadAsync(), channel.Reader.Completion.IsCompleted));

				// do something here?
			}

			var providerTask = Provide();

			try
			{
				await foreach (var s in source)
					await channel.Writer.WriteAsync(s).ConfigureAwait(false);

				channel.Writer.Complete();
			}
			catch (Exception e)
			{
				channel.Writer.Complete(e);
			}


			await providerTask;

			//return channel.Reader.ReadAllAsync(cancel);
		}





		public static async Task ServerProduce<T>(Func<IAsyncEnumerable<T>> source, Func<T, Task> target)
		{
			await foreach (var a in source().ConfigureAwait(false))
				await target(a).ConfigureAwait(false);
		}

		public static async Task ServerProduce<T>(Func<CancellationToken, IAsyncEnumerable<T>> source, Func<T, Task> target, CancellationToken cancel = default)
		{
			await foreach (var a in source(cancel).ConfigureAwait(false))
				await target(a).ConfigureAwait(false);
		}
	}

#endif

}
