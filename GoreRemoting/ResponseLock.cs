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
		// avoid random ObjectDisposedException
		if (_resultSent)
			throw new Exception("Too late, result sent");

		await _lock.EnterReadLockAsync().ConfigureAwait(false);

		if (_resultSent)
			throw new Exception("Too late, result sent");
	}

	public void ExitResponse() => _lock.ExitReadLock();
	public void Dispose() => _lock.Dispose();

	public async Task RundownResponsesAsync()
	{
		await _lock.EnterWriteLockAsync().ConfigureAwait(false);
		_resultSent = true;
		_lock.ExitWriteLock();
	}
}
