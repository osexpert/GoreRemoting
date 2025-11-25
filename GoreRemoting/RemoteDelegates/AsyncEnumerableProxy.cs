using System.Runtime.CompilerServices;

namespace GoreRemoting.RemoteDelegates;

/// <summary>
/// Proxy for IAsyncEnumerable that pulls data from remote client
/// </summary>
internal class AsyncEnumerableProxy
{
	public static IAsyncEnumerable<T> Create<T>(
		Func<Task<(T value, bool isDone)>> pullFunc
		)
	{
		return new AsyncEnumerableImpl<T>(pullFunc);
	}

	private class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
	{
		private readonly Func<Task<(T value, bool isDone)>> _pullFunc;
		bool _enumerated;

		public AsyncEnumerableImpl(Func<Task<(T value, bool isDone)>> pullFunc)
		{
			_pullFunc = pullFunc;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			if (_enumerated)
				throw new InvalidOperationException("This IAsyncEnumerable can only be enumerated once");
			_enumerated = true;

			return new AsyncEnumeratorImpl(_pullFunc, cancellationToken);
		}

		private class AsyncEnumeratorImpl : IAsyncEnumerator<T>
		{
			private readonly Func<Task<(T value, bool isDone)>> _pullFunc;
			private readonly CancellationToken _cancellationToken;
			private T _current = default!;
			private bool _isDone;

			public AsyncEnumeratorImpl(Func<Task<(T value, bool isDone)>> pullFunc, CancellationToken cancellationToken)
			{
				_pullFunc = pullFunc;
				_cancellationToken = cancellationToken;
			}

			public T Current => _current;

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_isDone)
					return false;

				_cancellationToken.ThrowIfCancellationRequested();  // Explicit check

				var (value, isDone) = await _pullFunc().ConfigureAwait(false);

				if (isDone)
				{
					_isDone = true;
					return false;
				}

				_current = value;
				return true;
			}

			public ValueTask DisposeAsync() => default;
		}
	}
}