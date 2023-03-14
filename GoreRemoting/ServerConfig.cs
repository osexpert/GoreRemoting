using Grpc.Core;
using GoreRemoting.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        public Func<Type, Metadata, object> CreateInstance { get; set; } = (t, m) => Activator.CreateInstance(t);

        public Dictionary<string, ISerializerAdapter> Serializers { get; } = new();// = Init();

        public ISerializerAdapter Serializer
        {
            get => Serializers.Values.Single();
            set
            {
                Serializers.Clear();
                AddSerializers(value);
			}
		}

		public void AddSerializers(params ISerializerAdapter[] adpaters)
        {
            foreach (var s in adpaters)
                Serializers.Add(s.Name, s);
        }

		//private static Dictionary<string, ISerializerAdapter> Init()
		//{
		//          var res = new Dictionary<string, ISerializerAdapter>();
		//          res.Add(_binaryFormatter.Name, _binaryFormatter);
		//          return res;
		//}

		// Use capacity of 1. We don't want to buffer anything, we just wanted to solve the problem of max 1 can write at a time,
		// the buffering was a side effect that I think may cause problems, at least unbounded, it may use all memory.
		public int? ResponseQueueLength { get; set; } = 1;
	}
}
