// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;

namespace GoreRemoting
{

	public static class ExceptionSerializationHelpers
	{
		static JsonSerializerOptions _jsonOptions = new();

		public static ExceptionData GetExceptionData(Exception ex)
		{
			Dictionary<string, string> propertyData = new();

			foreach (var p in ex.GetType().GetProperties())
			{
				if (p.Name == nameof(Exception.Message) 
					|| p.Name == nameof(Exception.StackTrace) 
					|| p.Name == nameof(Exception.TargetSite))
					continue;

				try
				{
					var value = JsonSerializer.SerializeToElement(p.GetValue(ex), _jsonOptions).ToString();
					propertyData.Add(p.Name, value);
				}
				catch
				{
					propertyData.Add(p.Name, p.GetValue(ex)?.ToString());
				}
			}

			// TODO: ex.ToString() will sometimes get more info??
			return new ExceptionData
			{
				Message = ex.Message,
				StackTrace = ex.StackTrace,
				TypeName = TypeShortener.GetShortType(ex.GetType()),
				ClassName = ex.GetType().ToString(),
				PropertyData = propertyData
			};
		}

		public static Exception RestoreAsRemoteInvocationException(ExceptionData ed)
		{
			var res = new RemoteInvocationException(ed.Message, ed.ClassName, ed.PropertyData);
			ExceptionHelper.SetRemoteStackTraceString(res, ed.StackTrace);
			ExceptionHelper.SetClassName(res, ed.ClassName);
			return res;
		}

		public static Exception RestoreWithGetUninitializedObject(ExceptionData ed)
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
				ExceptionHelper.SetRemoteStackTraceString(e, ed.StackTrace);
				ExceptionHelper.SetClassName(e, ed.ClassName);
				e.Data.Add(RemoteInvocationException.GoreRemotingPropertyDataKey, ed.PropertyData);

				return e;
			}
		}
	}

	public class ExceptionHelper
	{
		public static void SetRemoteStackTraceString(Exception e, string stackTrace)
		{
			FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(e, stackTrace);
		}

		public static void SetMessage(Exception e, string message)
		{
			var msgField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
			msgField.SetValue(e, message);
		}

		public static void SetClassName(Exception e, string className)
		{
			// does not exist in later .net versions
			var classNameField = typeof(Exception).GetField("_className ", BindingFlags.Instance | BindingFlags.NonPublic);
			classNameField?.SetValue(e, className);
		}
	}

	public class ExceptionData
	{
		public string Message;
		public string StackTrace;
		public string TypeName;
		public string ClassName;
		public Dictionary<string, string> PropertyData;
	}

	public enum ExceptionMarshalStrategy
	{
		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved)
		/// </summary>
		BinaryFormatter = 1,
		/// <summary>
		/// Same type, with only Message and StackTrace set
		/// </summary>
		UninitializedObject = 2,
		/// <summary>
		/// Always type RemoteInvocationException, with only Message, StackTrace and ClassName set
		/// </summary>
		RemoteInvocationException = 3
	}

}
