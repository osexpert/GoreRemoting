// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace GoreRemoting
{

	public static class ExceptionSerializationHelpers
	{
		public static ExceptionData GetExceptionData(Exception ex)
		{
			Dictionary<string, string> propertyData = new();

			foreach (var p in ex.GetType().GetProperties())
			{
				var val = p.GetValue(ex);
				if (val != null)
				{
					try
					{
						// Do not try to be smart, only write basic values.
						// Writing complete object graphs with eg. json may be tempting, but it can fail in various edge cases.
						// Better to just KISS.
						propertyData.Add(p.Name, XLinq_GetStringValue(val));
					}
					catch
					{
					}
				}
			}

			propertyData.Add(ExceptionData.ClassNameKey, ex.GetType().ToString());

			return new ExceptionData
			{
				TypeName = TypeShortener.GetShortType(ex.GetType()),
				PropertyData = propertyData
			};
		}

		/// <summary>
		/// https://github.com/microsoft/referencesource/blob/master/System.Xml.Linq/System/Xml/Linq/XLinq.cs
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		internal static string XLinq_GetStringValue(object value)
		{
			string s;
			if (value is string)
			{
				s = (string)value;
			}
			else if (value is double)
			{
				s = XmlConvert.ToString((double)value);
			}
			else if (value is float)
			{
				s = XmlConvert.ToString((float)value);
			}
			else if (value is decimal)
			{
				s = XmlConvert.ToString((decimal)value);
			}
			else if (value is bool)
			{
				s = XmlConvert.ToString((bool)value);
			}
			else if (value is DateTime)
			{
				s = GetDateTimeString((DateTime)value);
			}
			else if (value is DateTimeOffset)
			{
				s = XmlConvert.ToString((DateTimeOffset)value);
			}
			else if (value is TimeSpan)
			{
				s = XmlConvert.ToString((TimeSpan)value);
			}
			else if (value is XObject)
			{
				throw new ArgumentException();// Res.GetString(Res.Argument_XObjectValue));
			}
			else
			{
				s = value.ToString();
			}
			if (s == null) throw new ArgumentException();// Res.GetString(Res.Argument_ConvertToString));
			return s;
		}

		internal static string GetDateTimeString(DateTime value)
		{
			return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
		}

		public static Exception RestoreAsBinaryFormatter(Exception e)
		{
			ExceptionHelper.SetRemoteStackTrace(e, e.StackTrace);
			return e;
		}

		public static Exception RestoreAsRemoteInvocationException(ExceptionData ed)
		{
			var res = new RemoteInvocationException(ed);
			ExceptionHelper.SetRemoteStackTrace(res, ed.FullStackTrace());
			return res;
		}

		public static Exception RestoreAsUninitializedObject(ExceptionData ed)
		{
			var t = Type.GetType(ed.TypeName, false);
			if (t == null)
			{
				return RestoreAsRemoteInvocationException(ed);
			}
			else
			{
				if (!typeof(Exception).IsAssignableFrom(t))
				{
					throw new NotSupportedException($"Security check: {ed.TypeName} does not derive from Exception.");
				}

#if NETSTANDARD2_1_OR_GREATER
				var e = (Exception)RuntimeHelpers.GetUninitializedObject(t);
#else
				var e = (Exception)FormatterServices.GetUninitializedObject(t);
#endif

				ExceptionHelper.SetMessage(e, ed.Message);
				ExceptionHelper.SetRemoteStackTrace(e, ed.FullStackTrace());

				e.Data.Add(RemoteInvocationException.PropertyDataKey, ed.PropertyData);

				return e;
			}
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

	public class ExceptionData
	{
		public string TypeName;

		public const string MessageKey = nameof(Exception.Message);
		public const string StackTraceKey = nameof(Exception.StackTrace);
		public const string InnerExceptionKey = nameof(Exception.InnerException);
		public const string ClassNameKey = nameof(RemoteInvocationException.ClassName);

		public string GetValue(string key)
		{
			if (PropertyData.TryGetValue(key, out var value))
				return value;
			return null;
		}

		public string ClassName => GetValue(ClassNameKey);
		public string Message => GetValue(MessageKey);
		public string StackTrace => GetValue(StackTraceKey);
		public string InnerException => GetValue(InnerExceptionKey);

		public string FullStackTrace()
		{
			// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/exception.cs
			var ie = InnerException;
			if (ie != null)
				return " ---> " + ie + Environment.NewLine + "   " + "--- End of inner exception stack trace ---" + Environment.NewLine + StackTrace;
			else
				return StackTrace;
		}

		public Dictionary<string, string> PropertyData;
	}

	public enum ExceptionFormatStrategy
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

	public enum ExceptionFormat
	{
		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved, else serialized as UninitializedObject)
		/// </summary>
		BinaryFormatter = 1,
		/// <summary>
		/// Same type, with only Message, StackTrace and ClassName set (and PropertyData added to Data)
		/// </summary>
		UninitializedObject = 2,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace, ClassName and PropertyData set
		/// </summary>
		RemoteInvocationException = 3
	}
}
