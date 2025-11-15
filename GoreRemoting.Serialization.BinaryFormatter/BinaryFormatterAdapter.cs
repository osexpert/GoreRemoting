using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BF = System.Runtime.Serialization.Formatters.Binary;

namespace GoreRemoting.Serialization.BinaryFormatter;

/// <summary>
/// Serializer adapter to allow binary serialization.
/// </summary>
public class BinaryFormatterAdapter : ISerializerAdapter, IExceptionAdapter
{
	public string Name => "BinaryFormatter";

	[ThreadStatic]
	private static BF.BinaryFormatter _formatter;

	public BinarySerializerOptions Options { get; }

	/// <summary>
	/// Creates a new instance of the BinarySerializerAdapter class.
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER
	public BinaryFormatterAdapter(bool netCore = true)
#else
	public BinaryFormatterAdapter(bool netCore = false)
#endif
	{
		Options = CreateDefaultOptions(netCore);
	}

	public BinaryFormatterAdapter(BinarySerializerOptions options)
	{
		Options = options;
	}

	public ExceptionStrategy ExceptionStrategy { get; set; } = ExceptionStrategy.BinaryFormatter;

	public static BinarySerializerOptions CreateDefaultOptions(bool netCore)
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
		var dict = ExceptionSerialization.GetSerializableExceptionDictionary(ex);

		var ew = new ExceptionWrapper{ PropertyData = dict };

		// INFO: even if this is true, serialization may fail based on what is put in the Data-dictionary etc.
		if (ex.GetType().IsSerializable)
			ew.BinaryFormatterData = ex;

		return ew;
	}

	public Exception RestoreSerializedException(object ex, Func<Dictionary<string, string>, Exception> defaultHandler)
	{
		var ew = (ExceptionWrapper)ex;

		if (ExceptionStrategy == ExceptionStrategy.Default)
		{
			return defaultHandler(ew.PropertyData);
		}
		else if (ExceptionStrategy == ExceptionStrategy.BinaryFormatter)
		{
			if (ew.BinaryFormatterData != null)
				return RestoreAsBinaryFormatter(ew.BinaryFormatterData);
			else
				return defaultHandler(ew.PropertyData);
		}
		else
			throw new NotImplementedException("strategy: " + ExceptionStrategy); ;
	}

	public static Exception RestoreAsBinaryFormatter(Exception e)
	{
		ExceptionHelper.SetRemoteStackTrace(e, e.StackTrace);
		return e;
	}

	public class ExceptionHelper
	{
		private static void SetRemoteStackTraceString(Exception e, string stackTrace)
		{
			var remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(e, stackTrace);
		}

		//#if NET6_0_OR_GREATER
		//		public static void SetRemoteStackTrace(Exception e, string stackTrace) => ExceptionDispatchInfo.SetRemoteStackTrace(e, stackTrace);
		//#else
		public static void SetRemoteStackTrace(Exception e, string stackTrace)
		{
			//            if (!CanSetRemoteStackTrace())
			//          {
			//            return; // early-exit
			//      }

			// Store the provided text into the "remote" stack trace, following the same format SetCurrentStackTrace
			// would have generated.
			var _remoteStackTraceString = stackTrace + Environment.NewLine +
			//SR.Exception_EndStackTraceFromPreviousThrow 
			"--- End of stack trace from previous location ---"
			+ Environment.NewLine;

			SetRemoteStackTraceString(e, _remoteStackTraceString);
		}
		//#endif
		//public static void SetMessage(Exception e, string message)
		//{
		//	var msgField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
		//	msgField.SetValue(e, message);
		//}
	}

	[Serializable]
	class ExceptionWrapper
	{
		public Exception BinaryFormatterData { get; set; }
//			public string TypeName { get; set; }
		public Dictionary<string, string> PropertyData { get; set; }
	}

	public Type ExceptionType
	{
		get
		{
			//if (ExceptionStrategy == ExceptionStrategy.Default)
			//	return typeof(Dictionary<string, string>);
			//else
			return typeof(ExceptionWrapper);
		}
	}
	

	

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

public class BinarySerializerOptions(bool netCore)
{
	public bool NetCore { get; } = netCore;

	public BinarySerializerConfig? Config { get; set; } = null;

	public List<ISurrogate> Surrogates { get; } = new();

}

public enum ExceptionStrategy
{
	/// <summary>
	/// Use ExceptionSerialization.ExceptionStrategy setting
	/// </summary>
	Default = 0,

	//UninitializedObject = 1,
	///// <summary>
	///// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
	///// </summary>
	//RemoteInvocationException = 2,

	/// <summary>
	/// BinaryFormatter used (if serializable, everything is preserved, else serialized as default)
	/// </summary>
	BinaryFormatter = 3,
}
