using System;
using System.IO;
using System.IO.Compression;
using Grpc.Net.Compression;

namespace GoreRemoting;

/// <summary>
/// In case you want to disable compression on some methods.
/// </summary>
public class NoCompressionProvider : ICompressionProvider
{
	public string EncodingName => throw new NotSupportedException();

	public Stream CreateCompressionStream(Stream stream, CompressionLevel? compressionLevel)
	{
		throw new NotSupportedException();
	}

	public Stream CreateDecompressionStream(Stream stream)
	{
		throw new NotSupportedException();
	}
}
