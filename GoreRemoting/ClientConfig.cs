using Grpc.Core;
using GoreRemoting.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Grpc.Net.Compression;

namespace GoreRemoting
{
	/// <summary>
	/// Provides configuration settings for a CoreRemoting client instance.
	/// </summary>
	public class ClientConfig
	{
		/// <summary>
		/// Set to be notified before a method call
		/// </summary>BeforeMethodCall
		//      public ActionRef<Type, MethodInfo, Metadata, ISerializerAdapter, ICompressionProvider> BeforeMethodCall { get; set; }

		//public delegate void ActionRef<T1, T2, T3, T4, T5>(T1 a, T2 b, T3 c, ref T4 d, ref T5 e);
		public Action<BeforeCallArgs> BeforeCall { get; set; }

		public ClientConfig()
		{

		}


		public ClientConfig(params ISerializerAdapter[] serializers)
		{
			AddSerializer(serializers);
		}


		Type _defaultSerializer;

		/// <summary>
		/// If more than one serializer added, must specify here which one is default
		/// </summary>
		public Type DefaultSerializer
		{
			get
			{
				if (_defaultSerializer != null)
					return _defaultSerializer;

				// if we have only one, default is implied
				if (_serializers.Count() == 1)
					return _serializers.Single().Key;

				return null;
			}
			set
			{
				_defaultSerializer = value;
			}
		}

		private Dictionary<Type, ISerializerAdapter> _serializers = new();
		private Dictionary<string, ISerializerAdapter> _serializerByName = new();

		public void AddSerializer(params ISerializerAdapter[] serializers)
		{
			foreach (var serializer in serializers)
			{
				_serializers.Add(serializer.GetType(), serializer);
				_serializerByName.Add(serializer.Name, serializer);
			}
		}

		public ISerializerAdapter GetSerializerByType(Type serializer)
		{
			if (!typeof(ISerializerAdapter).IsAssignableFrom(serializer))
				throw new Exception("Not ISerializerAdapter");

			if (!_serializers.TryGetValue(serializer, out var res))
				throw new Exception("Serializer not found: " + serializer);

			return res;
		}

		public ICompressionProvider GetCompressorByType(Type compressor)
		{
			if (!typeof(ICompressionProvider).IsAssignableFrom(compressor))
				throw new Exception("Not ICompressionProvider");

			if (!_compressors.TryGetValue(compressor, out var res))
				throw new Exception("Compressor not found: " + compressor);

			return res;
		}

		internal ISerializerAdapter GetSerializerByName(string serializerName)
		{
			if (!_serializerByName.TryGetValue(serializerName, out var res))
				throw new Exception("Serializer not found: " + serializerName);

			return res;
		}

		internal ICompressionProvider GetCompressorByName(string compressorName)
		{
			if (!_compressorsByName.TryGetValue(compressorName, out var res))
				throw new Exception("Compressor not found: " + compressorName);

			return res;
		}

		public bool SetCallContext { get; set; } = true;

		public bool RestoreCallContext { get; set; } = true;


		private Dictionary<Type, ICompressionProvider> _compressors = new();
		private Dictionary<string, ICompressionProvider> _compressorsByName = new();


		Type _defaultCompressor;

		/// <summary>
		/// If more than one serializer added, must specify here which one is default
		/// </summary>
		public Type DefaultCompressor
		{
			get
			{
				if (_defaultCompressor != null)
					return _defaultCompressor;

				// if we have only one, default is implied
				if (_compressors.Count() == 1)
					return _compressors.Single().Key;

				return null;
			}
			set
			{
				_defaultCompressor = value;
			}
		}

		public void AddCompressor(params ICompressionProvider[] compressors)
		{
			foreach (var compressor in compressors)
			{
				_compressors.Add(compressor.GetType(), compressor);
				_compressorsByName.Add(compressor.EncodingName, compressor);
			}
		}
	}



	public class BeforeCallArgs
	{
		/// <summary>
		/// FIXME: does ref work here?
		/// </summary>
		/// <param name="type"></param>
		/// <param name="targetMethod"></param>
		/// <param name="headers"></param>
		public BeforeCallArgs(Type type, MethodInfo targetMethod, Metadata headers, ISerializerAdapter s, ICompressionProvider cp)
		{
			ServiceType = type;
			TargetMethod = targetMethod;
			Headers = headers;
			Compressor = cp;
			Serializer = s;
		}

		public Metadata Headers { get; }

		public Type ServiceType { get; }

		public MethodInfo TargetMethod { get; }

		public ISerializerAdapter Serializer { get; }
		public ICompressionProvider Compressor { get; }
	}
}
