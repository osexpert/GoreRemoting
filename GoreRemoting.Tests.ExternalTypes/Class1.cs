using System;
using System.Runtime.Serialization;
using MemoryPack;

namespace GoreRemoting.Tests.ExternalTypes
{
	[DataContract]
	[Serializable]
	[MemoryPackable]
	public partial class DataClass
	{
		[DataMember]
		public int Value { get; set; }
	}
}