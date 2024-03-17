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
		void Serialize(Stream stream, object?[] graph);

		object?[] Deserialize(Stream stream, Type[] types);

		object GetSerializableException(Exception ex);

		Exception RestoreSerializedException(object ex);

		string Name { get; }

		Type ExceptionType { get; }
	}
}
