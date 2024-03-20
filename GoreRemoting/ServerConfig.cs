using System;
using System.Collections.Generic;
using System.Reflection;
using GoreRemoting.Serialization;
using Grpc.Core;
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
		public Func<Type, ServerCallContext, object> GetService { get; set; } = GetServiceDefault;

		public static readonly Func<Type, ServerCallContext, object> GetServiceDefault = (serviceType, context) => Activator.CreateInstance(serviceType);

		public Func<ICallContext>? CreateCallContext { get; set; } = null;

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

		public bool EmitCallContext { get; set; } = true;
		public bool RestoreCallContext { get; set; } = true;

		//public ExceptionStrategy ExceptionStrategy => ExceptionStrategy.UninitializedObject;

		private Dictionary<string, ICompressionProvider> _compressors = new();

		public void AddCompressor(params ICompressionProvider[] compressors)
		{
			foreach (var compressor in compressors)
				_compressors.Add(compressor.EncodingName, compressor);
		}




		/// <summary>
		/// Gets or sets the sweep interval for inactive sessions in seconds (No session sweeping if set to 0).
		/// default: 1 minute
		/// </summary>
		public int InactiveSessionSweepIntervalSeconds { get; set; } = 60;

		/// <summary>
		/// Gets or sets the maximum session inactivity time in minutes.
		/// default: 5 minutes
		/// </summary>
		public int MaximumSessionInactivityTimeSeconds { get; set; } = 60 * 5;
	}

	public interface ICallContext : IDisposable
	{
		void Start(ServerCallContext context, string serviceName, string methodName, object service, MethodInfo method, object?[] parameterValues);

		void Success(object? result);
		void Failure(Exception exception);

	}

}
