using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BF = System.Runtime.Serialization.Formatters.Binary;

namespace GoreRemoting.Serialization.BinaryFormatter
{
    /// <summary>
    /// Serializer adapter to allow binary serialization.
    /// </summary>
    public class BinaryFormatterAdapter : ISerializerAdapter
    {

#if NETSTANDARD2_1_OR_GREATER
        public static bool NetCore { get; set; } = true;
#else
        public static bool NetCore { get; set; } = false;
#endif

        [ThreadStatic] 
        private static BF.BinaryFormatter _formatter;
        private readonly BinarySerializerConfig _config;
       
        /// <summary>
        /// Creates a new instance of the BinarySerializerAdapter class.
        /// </summary>
        /// <param name="config">Configuration settings</param>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public BinaryFormatterAdapter(BinarySerializerConfig config = null)
        {
            _config = config;
        }

        /// <summary>
        /// Gets a formatter instance.
        /// The instance is reused for further calls.
        /// </summary>
        /// <returns>Binary formatter instance</returns>
        private BF.BinaryFormatter GetFormatter()
        {
            if (_formatter == null)
            {
                _formatter = new BF.BinaryFormatter();

                if (_config != null)
                {
                    _formatter.TypeFormat = _config.TypeFormat;
                    _formatter.FilterLevel = _config.FilterLevel;
                    _formatter.AssemblyFormat = 
                        _config.SerializeAssemblyVersions 
                            ? System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full 
                            : System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                }
            }

            return _formatter;
        }

		/// <summary>
		/// Serializes an object graph.
		/// </summary>
		/// <param name="graph">Object graph to be serialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Serialized data</returns>
		public void Serialize(Stream s, object[] graph)
		{
			var binaryFormatter = GetFormatter();
			binaryFormatter.SerializeByteArray(s, graph);
		}

		/// <summary>
		/// Deserializes raw data back into an object graph.
		/// </summary>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <typeparam name="T">Object type</typeparam>
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream)
		{
			var binaryFormatter = GetFormatter();
			return (object[])binaryFormatter.DeserializeSafe(stream);
		}

		public object GetSerializableException(Exception ex2)
		{
            return ex2.GetType().IsSerializable ? ex2 : new RemoteInvocationException(ex2.Message); // TODO: set stack trace of RemoteInvocationException?
		}

		public Exception RestoreSerializedException(object ex2)
		{
            var e = (Exception)ex2;

			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(e, e.StackTrace + System.Environment.NewLine);

            return e;
		}

		public string Name => "BinaryFormatter";
    }

}