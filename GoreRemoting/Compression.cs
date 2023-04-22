using Grpc.Net.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GoreRemoting
{
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
}
