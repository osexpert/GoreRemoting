using MessagePack;
using System;
using System.IO;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		public MessagePackSerializerOptions Options { get; set; } = null;

		public void Serialize(Stream stream, object[] graph)
		{
			MessagePackSerializer.Serialize(stream, graph, Options);
		}

		public object[] Deserialize(Stream stream)
		{
			return MessagePackSerializer.Deserialize<object[]>(stream, Options);
		}

		public object GetSerializableException(Exception ex)
		{
			return ex;
		}

		public Exception RestoreSerializedException(object ex)
		{
			return (Exception)ex;
		}


	}
}
