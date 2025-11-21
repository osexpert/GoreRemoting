using System.Threading.Channels;

namespace GoreRemoting;

public static class AsyncEnumerableAdapter
{
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, Task> dataSource, 
		CancellationToken cancel = default
		)
	{
		var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = true
		});

		async Task ForwardAsync()
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

		//_ = Task.Run(ForwardAsync, cancel); Can do this if we want immediate return (wont have to wait until await dataSource completes)
		_ = ForwardAsync(); // fire and forget

		return channel.Reader.ReadAllAsync(cancel);
	}

	// Overload for data sources that accept cancellation
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, CancellationToken, Task> dataSource,
		CancellationToken cancel = default
		)
	{
		var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = true
		});

		async Task ForwardAsync()
		{
			try
			{
				await dataSource(data => channel.Writer.WriteAsync(data, cancel).AsTask(), cancel).ConfigureAwait(false);
				channel.Writer.Complete();
			}
			catch (Exception e)
			{
				channel.Writer.Complete(e);
			}
		}

		//_ = Task.Run(ForwardAsync, cancel); Can do this if we want immediate return (wont have to wait until await dataSource completes)
		_ = ForwardAsync();

		return channel.Reader.ReadAllAsync(cancel);
	}
}

public static class AsyncEnumerableExtensions
{
	public static async Task Push<T>(
		this IAsyncEnumerable<T> source,
		Func<T, Task> action,
		CancellationToken cancel = default
		)
	{
		await foreach (var item in source.WithCancellation(cancel).ConfigureAwait(false))
		{
			await action(item).ConfigureAwait(false);
		}
	}
}
