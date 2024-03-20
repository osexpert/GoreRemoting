﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using GoreRemoting.Serialization;

namespace GoreRemoting
{

	public static class ExceptionSerialization
	{
		public static ExceptionStrategy ExceptionStrategy => ExceptionStrategy.UninitializedObject;


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

			// Same logic as in dotnet:
			// Will include namespace but not full instantiation and assembly name.
			propertyData.Add(ExceptionData.ClassNameKey, ex.GetType().ToString());

			propertyData.Add(ExceptionData.TypeNameKey, TypeShortener.GetShortType(ex.GetType()));

			return new ExceptionData
			{
//				TypeName = ,
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
				throw new ArgumentException("XObjectValue");// Res.GetString(Res.Argument_XObjectValue));
			}
			else
			{
				s = value.ToString();
			}
			if (s == null) throw new ArgumentException("ConvertToString");// Res.GetString(Res.Argument_ConvertToString));
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

		public static Exception RestoreAsUninitializedObject(ExceptionData ed, Type? t)
		{
			// TODO: use AllowList and eg.ClassName (Type.ToString())

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

		public static Dictionary<string, string> GetSerializableExceptionDictionary(Exception ex)
		{
			return GetExceptionData(ex).PropertyData;
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

		private static Exception RestoreAsRemoteInvocationException(Dictionary<string, string> ex)
		{
			var ed = new ExceptionData() { PropertyData = ex };
			return ExceptionSerialization.RestoreAsRemoteInvocationException(ed);
		}

		private static Exception RestoreAsUninitializedObject(Dictionary<string, string> ex)
		{
			var ed = new ExceptionData() { PropertyData = ex };
			return ExceptionSerialization.RestoreAsUninitializedObject(ed, Type.GetType(ed.TypeName));
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
		

		public const string MessageKey = nameof(Exception.Message);
		public const string StackTraceKey = nameof(Exception.StackTrace);
		public const string InnerExceptionKey = nameof(Exception.InnerException);
		public const string ClassNameKey = nameof(RemoteInvocationException.ClassName);

		public const string TypeNameKey = nameof(ExceptionData.TypeName);

		public string? GetValue(string key)
		{
			if (PropertyData.TryGetValue(key, out var value))
				return value;
			return null;
		}

		public string ClassName => GetValue(ClassNameKey);
		public string Message => GetValue(MessageKey);
		public string StackTrace => GetValue(StackTraceKey);
		public string InnerException => GetValue(InnerExceptionKey);

		public string TypeName => GetValue(TypeNameKey);

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
