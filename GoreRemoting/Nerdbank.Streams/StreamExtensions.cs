// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.Streams
{
	using System;
	using System.Buffers;
	using System.IO;

	/// <summary>
	/// Stream extension methods.
	/// </summary>
	public static class StreamExtensions
	{


		/// <summary>
		/// Exposes a <see cref="ReadOnlySequence{T}"/> of <see cref="byte"/> as a <see cref="Stream"/>.
		/// </summary>
		/// <param name="readOnlySequence">The sequence of bytes to expose as a stream.</param>
		/// <returns>The readable stream.</returns>
		public static Stream AsStream(this ReadOnlySequence<byte> readOnlySequence) => new ReadOnlySequenceStream(readOnlySequence, null, null);

		/// <summary>
		/// Exposes a <see cref="ReadOnlySequence{T}"/> of <see cref="byte"/> as a <see cref="Stream"/>.
		/// </summary>
		/// <param name="readOnlySequence">The sequence of bytes to expose as a stream.</param>
		/// <param name="disposeAction">A delegate to invoke when the returned stream is disposed. This might be useful to recycle the buffers backing the <paramref name="readOnlySequence"/>.</param>
		/// <param name="disposeActionArg">The argument to pass to <paramref name="disposeAction"/>.</param>
		/// <returns>The readable stream.</returns>
		public static Stream AsStream(this ReadOnlySequence<byte> readOnlySequence, Action<object?>? disposeAction, object? disposeActionArg) => new ReadOnlySequenceStream(readOnlySequence, disposeAction, disposeActionArg);

		/// <summary>
		/// Creates a writable <see cref="Stream"/> that can be used to add to a <see cref="IBufferWriter{T}"/> of <see cref="byte"/>.
		/// </summary>
		/// <param name="writer">The buffer writer the stream should write to.</param>
		/// <returns>A <see cref="Stream"/>.</returns>
		public static Stream AsStream(this IBufferWriter<byte> writer) => new BufferWriterStream(writer);



	}
}
