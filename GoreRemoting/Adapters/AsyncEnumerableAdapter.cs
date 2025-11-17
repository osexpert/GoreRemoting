using System.Threading.Channels;

namespace GoreRemoting;

public static class AsyncEnumerableAdapter
{
	public static IAsyncEnumerable<T> Consume<T>(Func<Func<T, Task>, Task> dataSource, CancellationToken cancel = default)
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


	//public static async Task ClientProduce<T>(IAsyncEnumerable<T> source, Func<Func<Task<(T, bool)>>, Task> dataProvider)
	//{
	//	Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
	//	{
	//		SingleReader = true,
	//		SingleWriter = true
	//	});

	//	async Task Provide()
	//	{
	//		await dataProvider(async () => 
	//			(
	//				await channel.Reader.ReadAsync().ConfigureAwait(false), channel.Reader.Completion.IsCompleted
	//			)
	//		).ConfigureAwait(false);

	//		// do something here?
	//	}

	//	var providerTask = Provide();

	//	try
	//	{
	//		await foreach (var s in source.ConfigureAwait(false))
	//			await channel.Writer.WriteAsync(s).ConfigureAwait(false);

	//		channel.Writer.Complete();
	//	}
	//	catch (Exception e)
	//	{
	//		channel.Writer.Complete(e);
	//	}

	//	await providerTask.ConfigureAwait(false);

	//	//return channel.Reader.ReadAllAsync(cancel);
	//}

	//public static async Task Produce<T>(IAsyncEnumerable<T> source, Func<T, Task> target)
	//{
	//	await foreach (var a in source)
	//		await target(a).ConfigureAwait(false);
	//}

	public static async Task Produce<T>(IAsyncEnumerable<T> source, Func<T, Task> target, CancellationToken cancel = default)
	{
		await foreach (var a in source)
			await target(a).ConfigureAwait(false);
	}
}

//public static class AsyncEnumerableExtensions
//{
//	public static async Task ForEachAsync<T>(
//		this IAsyncEnumerable<T> source,
//		Func<T, Task> action,
//		CancellationToken cancellation = default)
//	{
//		await foreach (var item in source.WithCancellation(cancellation).ConfigureAwait(false))
//		{
//			await action(item).ConfigureAwait(false);
//		}
//	}
//}