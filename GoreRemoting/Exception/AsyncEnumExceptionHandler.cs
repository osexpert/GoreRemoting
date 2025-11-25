using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting;

class AsyncEnumerableExceptionHandler<T> : IAsyncEnumerable<T>
{
	readonly IAsyncEnumerator<T> _source;
	readonly Action<Exception> _exept;


	public AsyncEnumerableExceptionHandler(IAsyncEnumerator<T> source, Action<Exception> exept)
	{
		_source = source;
		_exept = exept;
	}

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancel = default)
	{
		return new AsyncEnumeratorExceptionHandler<T>(_source, _exept, cancel);
	}
}

class AsyncEnumeratorExceptionHandler<T> : IAsyncEnumerator<T>
{
	readonly CancellationToken _cancel;
	readonly IAsyncEnumerator<T> _source;
	readonly Action<Exception> _except;

	public AsyncEnumeratorExceptionHandler(IAsyncEnumerator<T> source, Action<Exception> exept, CancellationToken cancel)
	{
		_cancel = cancel;
		_except = exept;
		_source = source;
	}

	public T Current => _source.Current;

	public async ValueTask<bool> MoveNextAsync()
	{
		try
		{
			_cancel.ThrowIfCancellationRequested();

			var res = await _source.MoveNextAsync().ConfigureAwait(false);

			return res;

		}
		catch (Exception ex)
		{
			_except(ex);
			return false; // or throw???
		}

	}

	public ValueTask DisposeAsync()
	{
		//	_channel.Writer.TryComplete(); seems to not be needed
		return _source.DisposeAsync();
	}
}