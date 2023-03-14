using MessagePack;
using System;
using System.IO;

namespace GoreRemoting.Serialization.MessagePack
{
	public class MessagePackAdapter : ISerializerAdapter
	{
		public string Name => "MessagePack";

		public void Serialize(Stream s, object[] graph)
		{
			MessagePackSerializer.Serialize(s, graph);
		}

		public object[] Deserialize(Stream rawData)
		{
			return MessagePackSerializer.Deserialize<object[]>(rawData);
		}

		public object GetSerializableException(Exception ex2)
		{
			return ex2;
		}

		public Exception RestoreSerializedException(object ex2)
		{
			return (Exception)ex2;
		}


	}
}
