using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GoreRemoting
{
	public static class TypeShortener
	{
		// see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
		static readonly Regex AssemblyNameVersionSelectorRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=[\w-]+, PublicKeyToken=(?:null|[a-f0-9]{16})", RegexOptions.Compiled);
		static readonly ConcurrentDictionary<Type, string> typeNameCache = new ConcurrentDictionary<Type, string>();


		public static string GetShortType(Type type)
		{
			if (!typeNameCache.TryGetValue(type, out var typeName))
			{
				var full = type.AssemblyQualifiedName!;

				var shortened = AssemblyNameVersionSelectorRegex.Replace(full, string.Empty);

				// The regex handle generics, as you can see here (for non-generic types they will be the same, I think):
				//var n = type.FullName + ", " + type.Assembly.GetName().Name;
				// shortened: "System.Tuple`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib"
				// n: "System.Tuple`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib"
				//if (shortened != n)
				//throw new Exception("lol not the same");

				if (Type.GetType(shortened, false) == null)
				{
					// if type cannot be found with shortened name - use full name
					shortened = full;
				}

				typeNameCache[type] = shortened;
				typeName = shortened;
			}

			return typeName;
		}
	}
}
