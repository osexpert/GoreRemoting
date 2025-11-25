using System.Collections.Generic;
using System.Threading.Channels;

namespace GoreRemoting;

public static class AsyncEnumerableAdapter
{
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, Task> dataSource,
		CancellationToken cancel = default
		)
	{
		return FromPush<T>((a, cancel) => dataSource(a), cancel);
	}


	// Overload for data sources that accept cancellation
	public static IAsyncEnumerable<T> FromPush<T>(
		Func<Func<T, Task>, CancellationToken, Task> dataSource,
		CancellationToken cancel = default
		)
	{
		var channel = CreateChannel<T>(queueLength: 1);

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

		var source = channel.Reader.ReadAllAsync(cancel);//.GetAsyncEnumerator(cancel);
		return new AsyncEnumerableImplementation<T>(source, () => _ = ForwardAsync()); // fire and forget
	}


	class AsyncEnumerableImplementation<T> : IAsyncEnumerable<T>
	{
		readonly IAsyncEnumerable<T> _source;
		readonly Action _start;
		bool _enumerated;

		public AsyncEnumerableImplementation(IAsyncEnumerable<T> source, Action start)
		{
			_source = source;
			_start = start;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancel = default)
		{
			if (_enumerated)
				throw new InvalidOperationException("This IAsyncEnumerable can only be enumerated once");
			_enumerated = true;

			return new AsyncEnumeratorImplementation<T>(_source.GetAsyncEnumerator(cancel), _start, cancel);
		}
	}

	class AsyncEnumeratorImplementation<T> : IAsyncEnumerator<T>
	{
		readonly CancellationToken _cancel;
		readonly IAsyncEnumerator<T> _source;
		readonly Action _start;
		int _started;

		public AsyncEnumeratorImplementation(IAsyncEnumerator<T> source, Action start, CancellationToken cancel)
		{
			_cancel = cancel;
			_start = start;
			_source = source;
		}

		public T Current => _source.Current;

		public ValueTask<bool> MoveNextAsync()
		{
			_cancel.ThrowIfCancellationRequested();

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


	private static Channel<TT> CreateChannel<TT>(int? queueLength)
	{
		if (queueLength == null)
		{
			return Channel.CreateUnbounded<TT>(new UnboundedChannelOptions
			{
				SingleWriter = false,
				SingleReader = true,
			});
		}
		else
		{
			return Channel.CreateBounded<TT>(new BoundedChannelOptions(queueLength.Value)
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
