using Grpc.Core;
using GoreRemoting.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace GoreRemoting
{
    /// <summary>
    /// Provides configuration settings for a CoreRemoting client instance.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Set to be notified before a method call
        /// </summary>
        public ActionRef<Type, MethodInfo, Metadata, ISerializerAdapter> BeforeMethodCall { get; set; }

		public delegate void ActionRef<T1, T2, T3, T4>(T1 a, T2 b, T3 c, ref T4 d);

        public ClientConfig()
        {
            
        }


		public ClientConfig(params ISerializerAdapter[] serializers)
		{
            AddSerializers(serializers);
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

        public void AddSerializers(params ISerializerAdapter[] adapters)
        {
            foreach (var ada in adapters)
				_serializers.Add(ada.GetType(), ada);
        }

        public ISerializerAdapter GetSerializerByType(Type t)
        {
            if (!typeof(ISerializerAdapter).IsAssignableFrom(t))
                throw new Exception("Not ISerializerAdapter");

            if (!_serializers.TryGetValue(t, out var res))
                throw new Exception("Serializer not found: " + t);

            return res;
		}

        public bool SetCallContext { get; set; } = true;

		public bool RestoreCallContext { get; set; } = true;
	}
}
