using Grpc.Net.Compression;
using K4os.Compression.LZ4.Streams;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;

namespace GoreRemoting.Compression.Lz4
{
	/// <summary>
	/// https://github.com/grpc/grpc-dotnet/blob/master/src/Grpc.Net.Common/Compression/GzipCompressionProvider.cs
	/// </summary>
	public class Lz4CompressionProvider : ICompressionProvider
	{
		public string EncodingName => "lz4";

		public Stream CreateCompressionStream(Stream stream, CompressionLevel? compressionLevel)
		{
			return LZ4Stream.Encode(stream, leaveOpen: true);
		}

		public Stream CreateDecompressionStream(Stream stream)
		{
			return LZ4Stream.Decode(stream, leaveOpen: true);
		}
	}
}
