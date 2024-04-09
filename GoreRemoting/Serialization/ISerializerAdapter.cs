using System;
using System.IO;
using System.Reflection;

namespace GoreRemoting.Serialization
{
	/// <summary>
	/// Interface that serializer adapter components must implement.
	/// </summary>
	public interface ISerializerAdapter
	{
		void Serialize(Stream stream, object?[] graph, Type[] types);
		object?[] Deserialize(Stream stream, Type[] types);
		string Name { get; }
	}

	public interface IExceptionAdapter
	{
		object GetSerializableException(Exception ex);
		Exception RestoreSerializedException(object ex, Func<Dictionary<string, string>, Exception> defaultHandler);
		Type ExceptionType { get; }
	}
}
