using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GoreRemoting.Serialization.BinaryFormatter
{
	public static class PooledMemoryStream
	{
		private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public static MemoryStream GetStream() => _manager.GetStream();
	}
}
