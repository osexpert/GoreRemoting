using Grpc.Core;
using GoreRemoting.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Grpc.Net.Compression;

namespace GoreRemoting
{
	/// <summary>
	/// Describes the configuration settings of a CoreRemoting service instance.
	/// </summary>
	public class ServerConfig
	{
		/// <summary>
		/// Set this to overide the default Activator.CreateInstance
		/// </summary>
		public Func<Type, Metadata, object> CreateService { get; set; } = (t, m) => Activator.CreateInstance(t);

		private Dictionary<string, ISerializerAdapter> _serializers = new();

		public ServerConfig()
		{

		}

		public ServerConfig(params ISerializerAdapter[] serializers)
		{
			AddSerializer(serializers);
		}

		public void AddSerializer(params ISerializerAdapter[] serializers)
		{
			foreach (var serializer in serializers)
				_serializers.Add(serializer.Name, serializer);
		}

		internal ISerializerAdapter GetSerializerByName(string serializerName)
		{
			if (!_serializers.TryGetValue(serializerName, out var res))
				throw new Exception("Serializer not found: " + serializerName);

			return res;
		}

		internal ICompressionProvider GetCompressorByName(string compressorName)
		{
			if (!_compressors.TryGetValue(compressorName, out var res))
				throw new Exception("Compressor not found: " + compressorName);

			return res;
		}

		// Use capacity of 1. We don't want to buffer anything, we just wanted to solve the problem of max 1 can write at a time,
		// the buffering was a side effect that I think may cause problems, at least unbounded, it may use all memory.
		public int? ResponseQueueLength { get; set; } = 1;

		public bool SetCallContext { get; set; } = true;
		public bool RestoreCallContext { get; set; } = true;

		private Dictionary<string, ICompressionProvider> _compressors = new();

		public void AddCompressor(params ICompressionProvider[] compressors)
		{
			foreach (var compressor in compressors)
				_compressors.Add(compressor.EncodingName, compressor);
		}
	}
}
