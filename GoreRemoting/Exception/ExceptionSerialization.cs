// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using GoreRemoting.Serialization;

namespace GoreRemoting
{

	public static class ExceptionSerialization
	{
		public static ExceptionStrategy ExceptionStrategy => ExceptionStrategy.UninitializedObject;


		public static Dictionary<string, string> GetExceptionData(Exception ex)
		{
			var con = new ExceptionConverter(new JsonSerializerOptions());
			return con.Write(ex);

			//var ed = new ExceptionData();

			////foreach (var p in ex.GetType().GetProperties())
			////{
			////	var val = p.GetValue(ex);
			////	if (val != null)
			////	{
			////		// only write things we know we can (de)serialize?
			////		if (AllowedJsonType(val))
			////			ed.SetValue(p.Name, val);
			////		else if (val is Exception e)
			////			ed.SetValue(p.Name, e.ToString());

			////		//try
			////		//{
			////		//	// Do not try to be smart, only write basic values.
			////		//	// Writing complete object graphs with eg. json may be tempting, but it can fail in various edge cases.
			////		//	// Better to just KISS.
			////		//	propertyData.Add(p.Name, XLinq_GetStringValue(val));
			////		//}
			////		//catch
			////		//{
			////		//}
			////	}
			////}

			//ed.StackTrace = ex.StackTrace;
			//ed.Message = ex.Message;
			//ed.InnerException = ex.InnerException?.ToString();

			//// Same logic as in dotnet (ToString() on the type):
			//// Will include namespace but not full instantiation and assembly name.
			//ed.SetValue(ExceptionData.ClassNameKey, ex.GetType().ToString());

			//ed.SetValue(ExceptionData.TypeNameKey, TypeShortener.GetShortType(ex.GetType()));

			//return ed;
		}

		//private static bool AllowedJsonType(object val)
		//{
		//	return val is string || val is int || val is long || val is double || val is float || val is bool
		//		|| val is DateTime || val is decimal || val is TimeSpan || val is DateTimeOffset || val is Guid;
		//}

		///// <summary>
		///// https://github.com/microsoft/referencesource/blob/master/System.Xml.Linq/System/Xml/Linq/XLinq.cs
		///// </summary>
		///// <param name="value"></param>
		///// <returns></returns>
		///// <exception cref="ArgumentException"></exception>
		//internal static string XLinq_GetStringValue(object value)
		//{
		//	string s;
		//	if (value is string)
		//	{
		//		s = (string)value;
		//	}
		//	else if (value is double)
		//	{
		//		s = XmlConvert.ToString((double)value);
		//	}
		//	else if (value is float)
		//	{
		//		s = XmlConvert.ToString((float)value);
		//	}
		//	else if (value is decimal)
		//	{
		//		s = XmlConvert.ToString((decimal)value);
		//	}
		//	else if (value is bool)
		//	{
		//		s = XmlConvert.ToString((bool)value);
		//	}
		//	else if (value is DateTime)
		//	{
		//		s = GetDateTimeString((DateTime)value);
		//	}
		//	else if (value is DateTimeOffset)
		//	{
		//		s = XmlConvert.ToString((DateTimeOffset)value);
		//	}
		//	else if (value is TimeSpan)
		//	{
		//		s = XmlConvert.ToString((TimeSpan)value);
		//	}
		//	else if (value is XObject)
		//	{
		//		throw new ArgumentException("XObjectValue");// Res.GetString(Res.Argument_XObjectValue));
		//	}
		//	else
		//	{
		//		s = value.ToString();
		//	}
		//	if (s == null) throw new ArgumentException("ConvertToString");// Res.GetString(Res.Argument_ConvertToString));
		//	return s;
		//}

		//internal static string GetDateTimeString(DateTime value)
		//{
		//	return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
		//}

		public static Exception RestoreAsBinaryFormatter(Exception e)
		{
			ExceptionHelper.SetRemoteStackTrace(e, e.StackTrace);
			return e;
		}

		//private static Exception RestoreAsRemoteInvocationException(Dictionary<string, string> ex)
		//{
		//	var ed = new ExceptionData(ex);
		//	return ExceptionSerialization.RestoreAsRemoteInvocationException(ed);
		//}

		public static Exception RestoreAsRemoteInvocationException(Dictionary<string, string> dict)
		{
			var ed = new ExceptionData(dict);
			var res = new RemoteInvocationException(ed);
			ExceptionHelper.SetRemoteStackTrace(res, ed.FullStackTrace());
			return res;
		}

//		public static Exception RestoreAsUninitializedObject(ExceptionData ed, Type? t)
//		{
//			// TODO: use AllowList and eg.ClassName (Type.ToString())

//			if (t == null)
//			{
//				return RestoreAsRemoteInvocationException(ed);
//			}
//			else
//			{
//				if (!typeof(Exception).IsAssignableFrom(t))
//				{
//					throw new NotSupportedException($"Security check: {ed.TypeName} does not derive from Exception.");
//				}

//#if NETSTANDARD2_1_OR_GREATER
//				var e = (Exception)RuntimeHelpers.GetUninitializedObject(t);
//#else
//				var e = (Exception)FormatterServices.GetUninitializedObject(t);
//#endif

//				ExceptionHelper.SetMessage(e, ed.Message);
//				ExceptionHelper.SetRemoteStackTrace(e, ed.FullStackTrace());

//				e.Data.Add(RemoteInvocationException.PropertyDataKey, ed.PropertyData);

//				return e;
//			}
//		}

		public static Dictionary<string, string> GetSerializableExceptionDictionary(Exception ex)
		{
			//return GetExceptionData(ex).PropertyData;
			var con = new ExceptionConverter(new JsonSerializerOptions());
			return con.Write(ex);

		}

		public static Exception RestoreSerializedExceptionDictionary(Dictionary<string, string> ex)
		{
			return ExceptionStrategy switch
			{
				ExceptionStrategy.UninitializedObject => ExceptionSerialization.RestoreAsUninitializedObject(ex),
				ExceptionStrategy.RemoteInvocationException => ExceptionSerialization.RestoreAsRemoteInvocationException(ex),
				_ => throw new NotImplementedException()
			};
		}



		public static Exception RestoreAsUninitializedObject(Dictionary<string, string> ex)
		{

			//var ed = new ExceptionData(ex);

			//return ExceptionSerialization.RestoreAsUninitializedObject(ed, Type.GetType(ed.TypeName));
			var con = new ExceptionConverter(new JsonSerializerOptions());
			return con.Read(ex);
		}
	}

	public class ExceptionHelper
	{
		private static void SetRemoteStackTraceString(Exception e, string stackTrace)
		{
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(e, stackTrace);
		}

#if NET6_0_OR_GREATER
		public static void SetRemoteStackTrace(Exception e, string stackTrace) => ExceptionDispatchInfo.SetRemoteStackTrace(e, stackTrace);
#else
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
#endif
		public static void SetMessage(Exception e, string message)
		{
			var msgField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
			msgField.SetValue(e, message);
		}
	}

	//public class ExceptionSerializer
	//{
	//	public static void SetData(Exception e, ExceptionData data)
	//	{
	//		Set<string>(e, data, "Message", "_message");
	//		Set<System.Collections.IDictionary>(e, data, "Data", "_data");
	//		Set<Exception>(e, data, "InnerException", "_innerException");
	//		Set<string>(e, data, "HelpURL", "_helpURL");
	//		Set<string>(e, data, "StackTraceString", "_stackTraceString");
	//		Set<string>(e, data, "RemoteStackTraceString", "_remoteStackTraceString");
	//		Set<int>(e, data, "HResult", "_HResult");
	//		Set<int>(e, data, "Source", "_source");
	//	}

	//	private static void Set<T>(Exception e, ExceptionData data, string prop, string field)
	//	{
	//		try
	//		{
	//			var msgField = typeof(Exception).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
	//			msgField.SetValue(e, data.GetValue<T>(prop));
	//		}
	//		catch
	//		{
	//		}
	//	}
	//}

	public class ExceptionData
	{
		public const string MessageKey = nameof(Exception.Message);
		public const string StackTraceKey = nameof(Exception.StackTrace);
		public const string InnerExceptionKey = nameof(Exception.InnerException);
		public const string ClassNameKey = nameof(RemoteInvocationException.ClassName);
		public const string TypeNameKey = nameof(ExceptionData.TypeName);

		public ExceptionData()
		{
			PropertyData = new();
		}

		public ExceptionData(Dictionary<string, string> dict)
		{
			PropertyData = dict;
		}

		public string? GetValue(string key)
		{
			if (PropertyData.TryGetValue(key, out var value))
				return value;
			return null;
		}
		public void SetValue(string key, string? value)
		{
			PropertyData[key] = value;
		}

		public string ClassName
		{
			get => GetValue(ClassNameKey)!;
			set => SetValue(ClassNameKey, value);
		}

		public string Message
		{
			get => GetValue(MessageKey)!;
			set => SetValue(MessageKey, value);
		}
		
		public string StackTrace
		{
			get => GetValue(StackTraceKey)!;
			set => SetValue(StackTraceKey, value);
		}


		public string? InnerException
		{
			get => GetValue(InnerExceptionKey);
			set => SetValue(InnerExceptionKey, value);
		}

		public string TypeName
		{
			get => GetValue(TypeNameKey)!;
			set => SetValue(TypeNameKey, value);
		}

		public string FullStackTrace()
		{
			// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/exception.cs
			var ie = InnerException;
			if (string.IsNullOrEmpty(ie)) // protobuff: null becomes ""
			//if (ie == null)
				return StackTrace ?? string.Empty;
			else
				return " ---> " + ie + Environment.NewLine + "   " + "--- End of inner exception stack trace ---" + Environment.NewLine + StackTrace;
		}

		public Dictionary<string, string> PropertyData { get; }

		public T? GetValue<T>(string name)
		{
			var value = GetValue(name);
			if (value == null)
				return default;
			else if (typeof(T) == typeof(string))
				return (T)(object)value;
			else
				return JsonSerializer.Deserialize<T>(value);
		}

		public void SetValue<T>(string name, T? value)
		{
			if (value is null)
				SetValue(name, null);
			else if (value is string s)
				SetValue(name, s);
			else
				SetValue(name, JsonSerializer.Serialize<T>(value));
		}
	}

	public enum ExceptionStrategy
	{
		/// <summary>
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 1,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 2
	}
}
