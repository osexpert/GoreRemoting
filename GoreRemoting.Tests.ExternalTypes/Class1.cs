using System;
using System.Runtime.Serialization;
#if NET6_0_OR_GREATER
using MemoryPack;
#endif
using ProtoBuf;

namespace GoreRemoting.Tests.ExternalTypes;

[DataContract]
[Serializable]
#if NET6_0_OR_GREATER
[MemoryPackable]
#endif
[ProtoContract]
public partial class DataClass
{
	[DataMember]
	[ProtoMember(1)]
	public int Value { get; set; }
}
