using System;
using System.Runtime.Serialization;

namespace GoreRemoting.Tests.ExternalTypes
{
    [DataContract]
    [Serializable]
    public class DataClass
    {
        [DataMember]
        public int Value { get; set; }
    }
}