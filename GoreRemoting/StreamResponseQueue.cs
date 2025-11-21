using System.Threading.Channels;
using Grpc.Core;
using Channel = System.Threading.Channels.Channel;

namespace GoreRemoting;

/// <summary>
/// Wraps <see cref="IServerStreamWriter{T}"/> which only supports one writer at a time.
/// This class can receive messages from multiple threads, and writes them to the stream
/// one at a time.
/// 
/// fixes System.InvalidOperationException: 'Only one write can be pending at a time
/// https://github.com/grpc/grpc-dotnet/issues/579
/// https://github.com/grpc/grpc-dotnet/issues/579#issuecomment-574056565
/// 
/// </summary>
/// <typeparam name="T">Type of message written to the stream</typeparam>
public class StreamResponseQueue<T> : IDisposable
{
	private readonly IServerStreamWriter<T> _stream;
	private readonly Task _consumer;
	private readonly Channel<T> _channel;
	private CancellationTokenSource _ctsError = new();

	public CancellationToken OnErrorToken => _ctsError.Token;

	public StreamResponseQueue(
		IServerStreamWriter<T> stream,
		int? queueLimit = null,
		CancellationToken cancellationToken = default
	)
	{
		_channel = CreateChannel<T>(queueLimit);
		_stream = stream;
		_consumer = Consume(cancellationToken);
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

	/// <summary>
	/// Asynchronously writes an item to the channel.
	/// </summary>
	/// <param name="message">The value to write to the channel.</param>
	/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> used to cancel the write operation.</param>
	/// <returns>A <see cref="T:System.Threading.Tasks.ValueTask" /> that represents the asynchronous write operation.</returns>
	public async ValueTask WriteAsync(T message, CancellationToken cancellationToken = default)
	{
		await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Marks the writer as completed, and waits for all writes to complete.
	/// </summary>
	public Task CompleteAsync()
	{
		_channel.Writer.TryComplete(); // or Complete()?
		return _consumer;
	}

	private async Task Consume(CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
			{
				// Task WriteAsync(T message, CancellationToken cancellationToken) seems to always throw if handed a real cancelToken, so don't use it
				await _stream.WriteAsync(message).ConfigureAwait(false);
			}
		}
		catch (Exception e)
		{
			// could get a hang here, if we could not serialize the response (crash within the serializer itself)...during delegate callbacks.
			// Sounds similar to this: https://github.com/dotnet/runtime/issues/26235#issuecomment-436281070
			// So this only seem to happen with a bounded channel, where someone may be blocking on _channel.Writer.WriteAsync.
			// Completing the writer, in this case, will "cancel" the _channel.Writer.WriteAsync,
			// and it will not hang:
			// "If calling ChannelWriter<T>.Complete(Exception) will fault the channel immediately,
			// leading to pending WriteAsync calls to throw with the exception, then that's a perfectly acceptable solution."
			// This should have been mentioned in the docs IMO...

			// this seems to really fix it (as long as we cancel MoveNext with this)
			_ctsError.Cancel();

			// this seems to fix it...
			_channel.Writer.TryComplete(e); // or Complete(e)?
			throw;
		}
		finally
		{
			_ctsError.Dispose();
		}
	}

	public void Dispose()
	{
		_ctsError.Dispose();
	}

}
