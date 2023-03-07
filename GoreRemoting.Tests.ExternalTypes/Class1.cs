using MemoryPack;
using System;
using System.Runtime.Serialization;

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