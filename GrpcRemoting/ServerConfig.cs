using Grpc.Core;
using GrpcRemoting.Serialization;
using GrpcRemoting.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GrpcRemoting
{
    /// <summary>
    /// Describes the configuration settings of a CoreRemoting service instance.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Set this to overide the default Activator.CreateInstance
        /// </summary>
        public Func<Type, Metadata, object> CreateInstance;

		public bool EnableGrpcDotnetServerBidirStreamNotClosedHacks;
        public Action<ServerCallContext> GrpcDotnetServerBidirStreamNotClosedHackAction;

		static ISerializerAdapter _binaryFormatter = new BinarySerializerAdapter();

        public Dictionary<string, ISerializerAdapter> Serializers = Init();

		private static Dictionary<string, ISerializerAdapter> Init()
		{
            var res = new Dictionary<string, ISerializerAdapter>();
            res.Add(_binaryFormatter.Name, _binaryFormatter);
            return res;
		}
	}
}
