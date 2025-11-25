using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GoreRemoting;

class AsyncEnumerableExceptionHandler<T> : IAsyncEnumerable<T>
{
	readonly IAsyncEnumerable<T> _source; // Changed from IAsyncEnumerator
	readonly Action<Exception> _except;
	readonly bool _rethrowAfterHandling;

	public AsyncEnumerableExceptionHandler(
		IAsyncEnumerable<T> source,
		Action<Exception> except,
		bool rethrowAfterHandling = false)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_except = except ?? throw new ArgumentNullException(nameof(except));
		_rethrowAfterHandling = rethrowAfterHandling;
	}

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancel = default)
	{
		return new AsyncEnumeratorExceptionHandler<T>(
			_source.GetAsyncEnumerator(cancel),
			_except,
			_rethrowAfterHandling);
	}
}

class AsyncEnumeratorExceptionHandler<T> : IAsyncEnumerator<T>
{
	readonly IAsyncEnumerator<T> _source;
	readonly Action<Exception> _except;
	readonly bool _rethrowAfterHandling;

	public AsyncEnumeratorExceptionHandler(
		IAsyncEnumerator<T> source,
		Action<Exception> except,
		bool rethrowAfterHandling)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_except = except ?? throw new ArgumentNullException(nameof(except));
		_rethrowAfterHandling = rethrowAfterHandling;
	}

	public T Current => _source.Current;

	public async ValueTask<bool> MoveNextAsync()
	{
		try
		{
			return await _source.MoveNextAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_except(ex);

			if (_rethrowAfterHandling)
				throw;

			return false; // End enumeration on error
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			await _source.DisposeAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_except(ex);

			if (_rethrowAfterHandling)
				throw;
		}
	}
}

