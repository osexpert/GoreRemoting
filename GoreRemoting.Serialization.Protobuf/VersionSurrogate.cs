using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace GoreRemoting.Serialization.Protobuf
{
	[ProtoContract]
	class VersionSurrogate
	{
		[ProtoMember(1)]
		public string? VersionString { get; set; }

		[ProtoConverter]
		public static Version? ToVersion(VersionSurrogate vs)
		{
			if (vs.VersionString == null)
				return null;
			else
				return new Version(vs.VersionString);
		}

		[ProtoConverter]
		public static VersionSurrogate ToVersionSurrogate(Version? v)
		{
			if (v == null)
				return new VersionSurrogate { VersionString = null };
			else
				return new VersionSurrogate { VersionString = v.ToString() };
		}
	}
}
