using System;
using System.Collections.Generic;
using System.Text;
using KPreisser;

namespace GoreRemoting;

internal class ResponseLock : IDisposable
{
	AsyncReaderWriterLockSlim _lock = new AsyncReaderWriterLockSlim();

	volatile bool _resultSent;

	public async Task EnterResponseAsync()
	{
		try
		{
			await _lock.EnterReadLockAsync().ConfigureAwait(false);
		}
		catch (ObjectDisposedException)
		{
			throw new Exception("Too late, result sent");
		}

		if (_resultSent)
			throw new Exception("Too late, result sent");
	}

	public void ExitResponse() => _lock.ExitReadLock();
	public void Dispose()
	{
		try
		{
			_lock.Dispose();
		}
		//InvalidOperationException: At least one read lock was still active while trying to dispose the AsyncReaderWriterLockSlim.
		catch (InvalidOperationException)
		{
			//throw new Exception("Too late, result sent");
		}
	}

	public async Task RundownResponsesAsync()
	{
		await _lock.EnterWriteLockAsync().ConfigureAwait(false);
		_resultSent = true;
		_lock.ExitWriteLock();
	}
}
