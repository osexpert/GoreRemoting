/*
 * Based on code from Microsoft.Bot.Builder
 * https://github.com/CXuesong/BotBuilder.Standard
 * branch: netcore20+net45
 * BotBuilder.Standard/CSharp/Library/Microsoft.Bot.Builder/Fibers/NetStandardSerialization.cs
 * BotBuilder.Standard/CSharp/Library/Microsoft.Bot.Builder/Fibers/Serialization.cs
 */

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GoreRemoting.Serialization.BinaryFormatter
{

	public sealed class TypeSurrogate : ISurrogate
	{
		public bool Handles(Type type, StreamingContext context)
		{
			var handles = typeof(Type).IsAssignableFrom(type);
			return handles;
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
		{
			var type = (Type)obj;

			// BinaryFormatter in .NET Core 2.0 cannot persist types in System.Private.CoreLib.dll
			// that are not forwareded to mscorlib, including System.RuntimeType
			info.SetType(typeof(TypeReference));
			info.AddValue("TypeName", TypeShortener.GetShortType(type));
			//			info.AddValue("AssemblyName", type.Assembly.FullName);
			//		info.AddValue("FullName", type.FullName);
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context,
			ISurrogateSelector selector)
		{
			// Why go via IObjectReference? Why don't just use this?
			// I guess...this can be wasteful? BF has already made some object here and want us to set things in it.
			// But we just discard it. I guess IObjectReference may be how this was intended to be?
			throw new NotSupportedException();
			//var AssemblyQualifiedName = info.GetString("AssemblyQualifiedName");
			//return Type.GetType(AssemblyQualifiedName, true);
		}

		[Serializable]
		internal sealed class TypeReference : IObjectReference
		{

			//private readonly string AssemblyName;

			//private readonly string FullName;
			private readonly string TypeName;

			//public TypeReference(Type type)
			//{
			//	// But will this ctor ever be called?
			//	if (type == null) throw new ArgumentNullException(nameof(type));
			//	AssemblyName = type.Assembly.FullName;
			//	FullName = type.FullName;
			//}

			public object GetRealObject(StreamingContext context)
			{
				// Assembly.Load seems a bit too much?
				// Use TypeFormatter here too?
				//	var assembly = Assembly.Load(AssemblyName);
				//return assembly.GetType(FullName, true);
				return Type.GetType(TypeName, true);
			}
		}

	}
}
