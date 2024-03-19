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
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
#if NETSTANDARD2_1_OR_GREATER
		public BinaryFormatterAdapter(bool netCore = true)
#else
		public BinaryFormatterAdapter(bool netCore = false)
#endif
		{
			Options = GetOptions(netCore);
		}

		public ExceptionStrategy ExceptionStrategy { get; set; } = ExceptionStrategy.BinaryFormatterOrUninitializedObject;

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
		/// <returns>Serialized data</returns>
		public void Serialize(Stream stream, object?[] graph, Type[] types)
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
		/// <returns>Deserialized object graph</returns>
		public object[] Deserialize(Stream stream, Type[] types)
		{
			var binaryFormatter = GetFormatter();
			return (object[])DeserializeSafe(binaryFormatter, stream, Options);
		}

		/// <summary>
		/// Deserializes raw data back into an object.
		/// </summary>
		/// <param name="formatter">Binary formatter instance</param>
		/// <returns>Deserialized object</returns>
		private static object DeserializeSafe(BF.BinaryFormatter formatter, Stream stream, BinarySerializerOptions options)
		{
			var safeBinaryFormatter = formatter.Safe(options);
			return safeBinaryFormatter.Deserialize(stream);
		}

		public object GetSerializableException(Exception ex)
		{
			var ed = ExceptionSerializationHelpers.GetExceptionData(ex);
			var ew = ToExceptionWrapper(ed);

			// INFO: even if this is true, serialization may fail based on what is put in the Data-dictionary etc.
			if (ex.GetType().IsSerializable)
				ew.BinaryFormatterData = ex;

			return ew;
		}

		public Exception RestoreSerializedException(object ex)
		{
			var ew = (ExceptionWrapper)ex;

			if (ExceptionStrategy == ExceptionStrategy.BinaryFormatterOrUninitializedObject)
			{
				if (ew.BinaryFormatterData != null)
					return ExceptionSerializationHelpers.RestoreAsBinaryFormatter(ew.BinaryFormatterData);
				else
					return ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew), Type.GetType(ew.TypeName));
			}
			else if (ExceptionStrategy == ExceptionStrategy.BinaryFormatterOrRemoteInvocationException)
			{
				if (ew.BinaryFormatterData != null)
					return ExceptionSerializationHelpers.RestoreAsBinaryFormatter(ew.BinaryFormatterData);
				else
					return ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(ToExceptionData(ew));
			}
			else if (ExceptionStrategy == ExceptionStrategy.UninitializedObject)
			{
				return ExceptionSerializationHelpers.RestoreAsUninitializedObject(ToExceptionData(ew), Type.GetType(ew.TypeName));
			}
			else if (ExceptionStrategy == ExceptionStrategy.RemoteInvocationException)
			{
				return ExceptionSerializationHelpers.RestoreAsRemoteInvocationException(ToExceptionData(ew));
			}
			else
				throw new NotImplementedException("strategy: " + ExceptionStrategy); ;
		}

		private static ExceptionWrapper ToExceptionWrapper(ExceptionData ed)
		{
			return new ExceptionWrapper
			{
				TypeName = ed.TypeName,
				PropertyData = ed.PropertyData,
			};
		}

		private static ExceptionData ToExceptionData(ExceptionWrapper ew)
		{
			return new ExceptionData
			{
				TypeName = ew.TypeName,
				PropertyData = ew.PropertyData
			};
		}

		[Serializable]
		class ExceptionWrapper
		{
			public Exception BinaryFormatterData { get; set; }
			public string TypeName { get; set; }
			public Dictionary<string, string> PropertyData { get; set; }
		}

		public Type ExceptionType => typeof(ExceptionWrapper);

		public string Name => "BinaryFormatter";

		//public byte[] GetExceptionData(Exception e)
		//{
		//	if (!e.GetType().IsSerializable)
		//		throw new Exception("Not Serializable");

		//	var binaryFormatter = GetFormatter();
		//	using var ms = PooledMemoryStream.GetStream();
		//	SerializeSafe(binaryFormatter, ms, e, Options);
		//	return ms.ToArray();
		//}

		//public Exception RestoreException(byte[] data)
		//{
		//	var binaryFormatter = GetFormatter();
		//	using var ms = new MemoryStream(data);
		//	var e = (Exception)DeserializeSafe(binaryFormatter, ms, Options);
		//	ExceptionHelper.SetRemoteStackTrace(e, e.StackTrace);
		//	return e;
		//}

		//public object? Deserialize(Type type, object? value)
		//{
		//	return value;
		//}
	}

	public class BinarySerializerOptions
	{
		public bool NetCore { get; }

		public BinarySerializerOptions(bool netCore)
		{
			NetCore = netCore;
		}

		public BinarySerializerConfig? Config { get; set; } = null;

		public List<ISurrogate> Surrogates { get; } = new();

	}

	public enum ExceptionStrategy
	{
		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved, else serialized as UninitializedObject)
		/// </summary>
		BinaryFormatterOrUninitializedObject = 1,
		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved, else serialized as RemoteInvocationException)
		/// </summary>
		BinaryFormatterOrRemoteInvocationException = 2,
		/// <summary>
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 3,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 4
	}

}
