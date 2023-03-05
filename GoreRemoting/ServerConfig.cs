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

        //		static ISerializerAdapter _binaryFormatter = new BinarySerializerAdapter();

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

	}
}
