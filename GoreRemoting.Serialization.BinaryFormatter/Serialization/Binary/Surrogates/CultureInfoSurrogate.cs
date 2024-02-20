using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GoreRemoting.Serialization.BinaryFormatter
{
	internal class CultureInfoSurrogate : ISurrogate
	{
		public bool Handles(Type type, StreamingContext context)
		{
			var canHandle = type == typeof(CultureInfo);
			return canHandle;
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
		{
			var ci = (CultureInfo)obj;
			info.SetType(typeof(CultureInfoReference));
			info.AddValue("Name", ci.Name);
			info.AddValue("UseUserOverride", ci.UseUserOverride);
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
		{
			//return new CultureInfo(info.GetString("Name"), info.GetBoolean("UseUserOverride"));
			throw new NotSupportedException();
		}

		[Serializable]
		internal sealed class CultureInfoReference : IObjectReference
		{
			private readonly string Name;
			private readonly bool UseUserOverride;

			public object GetRealObject(StreamingContext context)
			{
				return new CultureInfo(Name, UseUserOverride);
			}
		}
	}
}
