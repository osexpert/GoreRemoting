using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GoreRemoting;

internal static class AsyncEnumerableHelper
{
	public static bool IsAsyncEnumerable(IRemotingParty r, Type type, [NotNullWhen(true)] out Type? elementType)
	{
		if (r.AsyncEnuTypesCache.TryGetValue(type, out elementType))
		{
			return elementType != null;
		}

		var res = IsAsyncEnumerable(type, out elementType);
		
		r.AsyncEnuTypesCache.TryAdd(type, elementType);
		return res;
	}


	public static bool IsAsyncEnumerable(Type type, [NotNullWhen(true)] out Type? elementType)
	{
		// direct type?
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
		{
			elementType = type.GetGenericArguments()[0];
			return true;
		}

		// implemented / inherited?
		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
			{
				elementType = iface.GetGenericArguments()[0];
				return true;
			}
		}

		elementType = null;
		return false;
	}
}
