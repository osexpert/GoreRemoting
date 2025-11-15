#if NET5_0_OR_GREATER
#else

namespace MemoryPack
{
	internal class MemoryPackableAttribute : Attribute
	{
		internal MemoryPackableAttribute()
		{
		}
		internal MemoryPackableAttribute(GenerateType gt)
		{
		}
	}
	internal class MemoryPackConstructor : Attribute
	{
	}
	internal enum GenerateType
	{
		CircularReference
	}
	internal class MemoryPackOrderAttribute : Attribute
	{
		internal MemoryPackOrderAttribute(int o)
		{
		}
	}
}

//namespace GoreRemoting.Serialization.MemoryPack
//{
//	public class MemoryPackAdapter : GoreRemoting.Serialization.ISerializerAdapter
//	{
//		public void Serialize(Stream stream, object?[] graph, Type[] types)
//		{
//		}
//		public object?[] Deserialize(Stream stream, Type[] types)
//		{
//			return [];
//		}
//		public string Name => "ff";
//	}
//}

#endif
