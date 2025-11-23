using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace GoreRemoting;

#if NETSTANDARD2_0
public static class ChannelReaderExtensions
{
	//
	// Summary:
	//     Creates an System.Collections.Generic.IAsyncEnumerable`1 that enables reading
	//     all of the data from the channel.
	//
	// Parameters:
	//   cancellationToken:
	//     The cancellation token to use to cancel the enumeration. If data is immediately
	//     ready for reading, then that data may be yielded even after cancellation has
	//     been requested.
	//
	// Returns:
	//     The created async enumerable.
	public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> cr, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		while (await cr.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
		{
			while (cr.TryRead(out T? item))
			{
				yield return item;
			}
		}
	}
}
#endif
