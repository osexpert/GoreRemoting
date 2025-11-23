using System.Collections.Generic;
using System.Threading.Channels;

namespace GoreRemoting;

public static class AsyncEnumerableAdapter
{
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, Task> dataSource,
		int? queueLimit = null,
		CancellationToken cancel = default
		)
	{
		return FromPush<T>((a, cancel) => dataSource(a), queueLimit, cancel);
	}


	// Overload for data sources that accept cancellation
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, CancellationToken, Task> dataSource,
		int? queueLimit = null,
		CancellationToken cancel = default
		)
	{
		var channel = CreateChannel<T>(queueLimit);

		async Task ForwardAsync()
		{
			try
			{
				await dataSource(data => channel.Writer.WriteAsync(data, cancel).AsTask(), cancel).ConfigureAwait(false);
				channel.Writer.Complete();
			}
			catch (Exception e)
			{
				channel.Writer.TryComplete(e);
			}
		}

		bool delayed = true;
		if (delayed)
		{
			return new AsyncEnumerableImplementation<T>(channel, () => _ = ForwardAsync());
		}
		else
		{
			//_ = Task.Run(ForwardAsync, cancel); Can do this if we want immediate return (wont have to wait until await dataSource completes)
			_ = ForwardAsync(); // fire and forget
			return channel.Reader.ReadAllAsync(cancel);
		}
	}


	class AsyncEnumerableImplementation<T> : IAsyncEnumerable<T>
	{
		readonly Channel<T> _channel;
		readonly Action _start;
		bool _enumerated;

		public AsyncEnumerableImplementation(Channel<T> channel, Action start)
		{
			_channel = channel;
			_start = start;
		}
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancel = default)
		{
			if (_enumerated)
				throw new InvalidOperationException("This IAsyncEnumerable can only be enumerated once");
			_enumerated = true;

			return new AsyncEnumeratorImplementation<T>(_channel, _start, cancel);
		}
	}

	class AsyncEnumeratorImplementation<T> : IAsyncEnumerator<T>
	{
		readonly IAsyncEnumerator<T> _source;
		readonly Channel<T> _channel;
		readonly Action _start;
		int _started;

		public AsyncEnumeratorImplementation(Channel<T> channel, Action start, CancellationToken cancel)
		{
			_start = start;
			_channel = channel;
			_source = _channel.Reader.ReadAllAsync(cancel).GetAsyncEnumerator(cancel);
		}

		public T Current => _source.Current;

		public ValueTask<bool> MoveNextAsync()
		{
			if (Interlocked.Exchange(ref _started, 1) == 0)
				_start();

			return _source.MoveNextAsync();
		}

		public ValueTask DisposeAsync()
		{
		//	_channel.Writer.TryComplete(); seems to not be needed
			return _source.DisposeAsync();
		}
	}


	private static Channel<TT> CreateChannel<TT>(int? queueLimit)
	{
		if (queueLimit == null)
		{
			return Channel.CreateUnbounded<TT>(new UnboundedChannelOptions
			{
				SingleWriter = false,
				SingleReader = true,
			});
		}
		else
		{
			return Channel.CreateBounded<TT>(new BoundedChannelOptions(queueLimit.Value)
			{
				SingleWriter = false,
				SingleReader = true
			});
		}
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
