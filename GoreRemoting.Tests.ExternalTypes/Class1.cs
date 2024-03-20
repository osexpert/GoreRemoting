using System;
using System.Runtime.Serialization;
using MemoryPack;
using ProtoBuf;

namespace GoreRemoting.Tests.ExternalTypes
{
	[DataContract]
	[Serializable]
	[MemoryPackable]
	[ProtoContract]
	public partial class DataClass
	{
		[DataMember]
		[ProtoMember(1)]
		public int Value { get; set; }
	}
}
