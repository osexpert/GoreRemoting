using System.IO;
using Microsoft.IO;

namespace GoreRemoting.Serialization.BinaryFormatter
{
	public static class PooledMemoryStream
	{
		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public static MemoryStream GetStream() => _manager.GetStream();
	}
}
