using System;
using System.Collections.Generic;
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

        [ThreadStatic] 
        private static BF.BinaryFormatter _formatter;

        public BinarySerializerOptions Options { get; }
       
        /// <summary>
        /// Creates a new instance of the BinarySerializerAdapter class.
        /// </summary>
        /// <param name="config">Configuration settings</param>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
#if NETSTANDARD2_1_OR_GREATER
		public BinaryFormatterAdapter(bool netCore = true)
#else
        public BinaryFormatterAdapter(bool netCore = false)
#endif
		{
			Options = GetOptions(netCore);
        }

		private static BinarySerializerOptions GetOptions(bool netCore)
		{
			var opt = new BinarySerializerOptions(netCore);

			// These 2 are about safety during deserialize (does not alter serialization)
			opt.Surrogates.Add(new DataSetSurrogate(opt));
			opt.Surrogates.Add(new WindowsIdentitySurrogate(opt));

			if (netCore)
			{
				// These are about things that are no longer Serializable in net6
				// There is a lot more that is not Serializable in net6 (CollectionBase etc.)
				// but for some things its easier\better to change to somethign else than adding support for it here.
				opt.Surrogates.Add(new TypeSurrogate());
				opt.Surrogates.Add(new CultureInfoSurrogate());
			}

			return opt;
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

                if (Options.Config != null)
                {
                    _formatter.TypeFormat = Options.Config.TypeFormat;
                    _formatter.FilterLevel = Options.Config.FilterLevel;
                    _formatter.AssemblyFormat =
						Options.Config.SerializeAssemblyVersions 
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
		public void Serialize(Stream stream, object[] graph)
		{
			var binaryFormatter = GetFormatter();
            SerializeSafe(binaryFormatter, stream, graph, Options);
		}

		/// <summary>
		/// Serializes the specified object into a byte array.
		/// </summary>
		/// <param name="formatter">Binary formatter instance</param>
		/// <param name="objectToSerialize">Object to serialize</param>
		/// <returns>Serialized data</returns>
		private static void SerializeSafe(BF.BinaryFormatter formatter, Stream stream, object objectToSerialize, BinarySerializerOptions options)
		{
			if (options.NetCore)
			{
				// for net6 need to use "Safe" formatter to enable the surrogates for types no longer serializable
				// Yes...because DataSet and WindowsIdentity only need the safety during deserialize. But I guess it does not hurt to use it in all cases,
				// so make it always active.

				var safeBinaryFormatter = formatter.Safe(options);
				safeBinaryFormatter.Serialize(stream, objectToSerialize);
			}
			else
			{
				formatter.Serialize(stream, objectToSerialize);
			}
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
			return (object[])DeserializeSafe(binaryFormatter, stream, Options);
		}

		/// <summary>
		/// Deserializes raw data back into an object.
		/// </summary>
		/// <param name="formatter">Binary formatter instance</param>
		/// <param name="rawData">Raw data that should be deserialized</param>
		/// <returns>Deserialized object</returns>
		private static object DeserializeSafe(BF.BinaryFormatter formatter, Stream stream, BinarySerializerOptions options)
		{
			var safeBinaryFormatter = formatter.Safe(options);
			return safeBinaryFormatter.Deserialize(stream);
		}

		public object GetSerializableException(Exception ex)
		{
			if (ex.GetType().IsSerializable)
				return ex;

			var res = new RemoteInvocationException(ex.Message, TypeShortener.GetShortType(ex.GetType()));

			// TODO: set stack trace of RemoteInvocationException? yes!
			// TODO: check if this works
			FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
			remoteStackTraceString.SetValue(res, ex.StackTrace);

			return res;
		}

		public Exception RestoreSerializedException(object ex)
		{
            var e = (Exception)ex;

			FieldInfo remoteStackTraceString = ExceptionHelper.GetRemoteStackTraceString();
			remoteStackTraceString.SetValue(e, e.StackTrace + System.Environment.NewLine);

            return e;
		}

		public string Name => "BinaryFormatter";
    }

    public class BinarySerializerOptions
    {
		public bool NetCore { get; }

        public BinarySerializerOptions(bool netCore)
        {
			NetCore = netCore;
        }

        public BinarySerializerConfig Config { get; set; } = null;

		public List<ISurrogate> Surrogates { get; } = new();

	}

}