using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace GoreRemoting.Serialization.Protobuf
{
	[ProtoContract]
	struct DateTimeOffsetSurrogate
	{
		[ProtoMember(1)]
		public DateTime DateTime { get; set; }
		[ProtoMember(2)]
		public TimeSpan Offset { get; set; }

		//[ProtoConverter]
		//public static DateTimeOffset? ToVersion(DateTimeOffsetSurrogate? vs)
		//{
		//	if (vs == null)
		//		return null;
		//	else
		//		return new DateTimeOffset(vs.Value.DateTime, vs.Value.Offset);
		//	//if (vs.VersionString == null)
		//	//	return null;
		//	//else
		//	//	return new Version(vs.VersionString);
		//}

		//[ProtoConverter]
		//public static DateTimeOffsetSurrogate? ToVersionSurrogate(DateTimeOffset? v)
		//{
		//	if (v == null)
		//		return null;
		//	else
		//		return new DateTimeOffsetSurrogate() { DateTime = v.Value.DateTime, Offset = v.Value.Offset }; 
		//}

		[ProtoConverter]
		public static DateTimeOffset ToVersion(DateTimeOffsetSurrogate vs)
		{
			return new DateTimeOffset(vs.DateTime, vs.Offset);
			//if (vs.VersionString == null)
			//	return null;
			//else
			//	return new Version(vs.VersionString);
		}

		[ProtoConverter]
		public static DateTimeOffsetSurrogate ToVersionSurrogate(DateTimeOffset v)
		{
			return new DateTimeOffsetSurrogate() { DateTime = v.DateTime, Offset = v.Offset };
		}
	}


	
}
