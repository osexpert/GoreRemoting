using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using Grpc.Net.Compression;

namespace GoreRemoting
{
	public interface IGoreializable
	{
		void Serialize(GoreBinaryWriter w, Stack<object?> st);
		void Deserialize(GoreBinaryReader r);
		void Deserialize(Stack<object?> st);
	}

	public interface IMessage : IGoreializable
	{
		MessageType MessageType { get; }

		/// <summary>
		/// A value to separate messages of same type (for the same method).
		/// </summary>
		int CacheKey { get; }
	}

	public enum MessageType
	{
		MethodCall = 1,
		MethodResult = 2,
		DelegateCall = 3,
		DelegateResult = 4,
	}

	internal class Goreializer
	{

		public static void Serialize(IRemotingParty r, Stream ms, MethodInfo method, IMessage msg, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			//using var ms = PooledMemoryStream.GetStream();

			var cs = GetCompressor(compressor, ms) ?? ms;
			try
			{
				var stack = new Stack<object?>();

				var bw = new GoreBinaryWriter(cs);
				msg.Serialize(bw, stack);

				if (stack.Any())
				{ 
					var types = GetTypes(r, method, msg, serializer);
					// We can't get the elements out in the "correct" order...how they are stored internally...
					serializer.Serialize(cs, stack.Reverse().ToArray(), types);
				}
			}
			finally
			{
				if (cs != ms)
					cs.Dispose();
			}

			//return ms.ToArray();
		}

		private static Stream? GetCompressor(ICompressionProvider? compressor, Stream ms)
		{
			if (compressor != null)
				return compressor.CreateCompressionStream(ms, System.IO.Compression.CompressionLevel.Fastest);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}

		public static T Deserialize<T>(IRemotingParty r, Stream ms, MethodInfo method, ISerializerAdapter serializer, ICompressionProvider? compressor) 
			where T : IMessage, new()
		{
			var msg = new T();

			var ds = GetDecompressor(compressor, ms) ?? ms;
			try
			{
				var br = new GoreBinaryReader(ds);
				msg.Deserialize(br);

				var types = GetTypes(r, method, msg, serializer);

				if (types.Any())
				{
					object?[] arr = serializer.Deserialize(ds, types);
					msg.Deserialize(new Stack<object?>(arr.Reverse()));
				}
			}
			finally
			{
				if (ds != ms)
					ds.Dispose();
			}

			return msg;
		}

		private static Type[] GetTypes(IRemotingParty r, MethodInfo method, IMessage msg, ISerializerAdapter serializer)
		{
			if (r.TypesCache.TryGetValue((method, msg.MessageType, msg.CacheKey), out var types))
				return types;

			types = GetTypesUncached(method, msg, serializer);
			r.TypesCache.TryAdd((method, msg.MessageType, msg.CacheKey), types);

			return types;
		}

		private static Type[] GetTypesUncached(MethodInfo method, IMessage msg, ISerializerAdapter serializer)
		{
			var param_s = method.GetParameters();

			if (msg is MethodResultMessage mrm)
			{
				if (mrm.ResultType == MethodResultType.Exception)
				{
					return new Type[] { Goreializer.GetExceptionType(serializer) };
				}
				else if (mrm.ResultType == MethodResultType.Exception_dict_internal)
				{
					return new Type[] { };
				}
				else
				{
					var l = new List<Type>();

					Type retType = method.ReturnType;

					// we know this from ResultType too...
					// we know this from ResultType too... == typeof(ValueTask);
					var isVoid = retType == typeof(void) || retType == typeof(Task) || retType == typeof(ValueTask);
					if (!isVoid)
					{
						if (retType.IsGenericType)
						{
							var gtd = method.ReturnType.GetGenericTypeDefinition();
							if (gtd == typeof(ValueTask<>) || gtd == typeof(Task<>))
							{
								retType = method.ReturnType.GenericTypeArguments.Single();
							}
						}

						l.Add(retType);
					}

					l.AddRange(mrm.OutArguments
						.Select(oa => param_s[oa.Position])
						.Select(p => p.IsOutParameterForReal() ? p.ParameterType.GetElementType() : p.ParameterType));

					//mrm.OutArguments.Select(oa => method.GetParameters() oa.ParameterName)

					return l.ToArray();
				}
			}
			else if (msg is MethodCallMessage mcm)
			{
				var types =	param_s
					//						.Select(p => p.ParameterType)
					.Where(p =>
					{
						if (p.IsOutParameterForReal()
							|| typeof(Delegate).IsAssignableFrom(p.ParameterType)
							|| typeof(CancellationToken).IsAssignableFrom(p.ParameterType)
							)
							return false;

						return true;
					})
					.Select(t =>
					{
						//if (t.IsByRef)
						//	t = t.GetElementType();
						//else if (typeof(Delegate).IsAssignableFrom(t))
						//{
						//	//var invokeMethod = t.GetMethod("Invoke");
						//	//var retType = invokeMethod.ReturnType;
						//	t = typeof(string);//  retType; it does not matter the type. it will always be null anyways, but it can not be void...
						//}


						return t.ParameterType;
					})
					.ToArray();
				return types;
			}
			else if (msg is DelegateCallMessage dcm)
			{
				// what if delegate without args`? Func vs Action...
				//arr = serializer.Deserialize(ds, param_s[dcm.Position].ParameterType.GenericTypeArguments.Skip(1).ToArray());

				var delegateType = param_s[dcm.Position].ParameterType;
				var invokeMethod = delegateType.GetMethod("Invoke");
				//arr = serializer.Deserialize(ds, invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
				return invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();

			}
			else if (msg is DelegateResultMessage drm)
			{
				if (drm.ResultType == DelegateResultType.Exception)
				{
					return new Type[] { Goreializer.GetExceptionType(serializer) };
				}
				else if (drm.ResultType == DelegateResultType.Exception_dict_internal)
				{
					return new Type[] { };
				}
				else
				{
					var delegateType = param_s[drm.Position].ParameterType;
					var invokeMethod = delegateType.GetMethod("Invoke");

					var retType = invokeMethod.ReturnType;

					if (invokeMethod.ReturnType.IsGenericType)
					{
						var gtd = invokeMethod.ReturnType.GetGenericTypeDefinition();
						if (gtd == typeof(ValueTask<>) || gtd == typeof(Task<>))
						{
							retType = invokeMethod.ReturnType.GenericTypeArguments.Single();
						}
					}

					return new Type[] { retType };
				}
			}
			else
				throw new Exception();
		}

		private static Stream? GetDecompressor(ICompressionProvider? compressor, Stream ms)
		{
			if (compressor != null)
				return compressor.CreateDecompressionStream(ms);
			else
				return null;// new NonDisposablePassthruStream(ms);
		}

		private static Type GetExceptionType(ISerializerAdapter serializer)
		{
			if (serializer is IExceptionAdapter ea)
				return ea.ExceptionType;
			else
				return typeof(Dictionary<string, string>);
		}

		internal static Exception RestoreSerializedException(ExceptionStrategy exs, ISerializerAdapter serializer, object ex)
		{
			if (serializer is IExceptionAdapter ea)
				return ea.RestoreSerializedException(ex, dict => ExceptionSerialization.RestoreSerializedExceptionDictionary(exs, dict));
			else
				return ExceptionSerialization.RestoreSerializedExceptionDictionary(exs, (Dictionary<string, string>)ex);
		}

		internal static object GetSerializableException(ISerializerAdapter serializer, Exception ex)
		{
			if (serializer is IExceptionAdapter ea)
				return ea.GetSerializableException(ex);
			else
				return ExceptionSerialization.GetSerializableExceptionDictionary(ex);
		}
	
	}

	public class GoreBinaryWriter : BinaryWriter
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryWriter(Stream outp) : base(outp, _utf8NoBom, leaveOpen: true)
		{

		}

		public void WriteVarInt(int i) => base.Write7BitEncodedInt(i);

		public void Write(Guid g) => Write(g.ToByteArray());

		public void WriteNullableString(string? str)
		{
			var hasValue = str != null;
			Write(hasValue);
			if (hasValue)
				Write(str);
		}
	}

	public class GoreBinaryReader : BinaryReader
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryReader(Stream inp) : base(inp, _utf8NoBom, leaveOpen: true)
		{
		}

		public int ReadVarInt() => base.Read7BitEncodedInt();

		public Guid ReadGuid() => new Guid(ReadBytes(16));

		public string? ReadNullableString()
		{
			return ReadBoolean() ? ReadString() : null;
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Interface | AttributeTargets.Method)]
	public class SerializerAttribute : Attribute
	{
		public Type Serializer { get; }

		public SerializerAttribute(Type t)
		{
			if (!typeof(ISerializerAdapter).IsAssignableFrom(t))
				throw new Exception("Not ISerializerAdapter");

			Serializer = t;
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Interface | AttributeTargets.Method)]
	public class CompressorAttribute : Attribute
	{
		public Type Compressor { get; }

		public CompressorAttribute(Type t)
		{
			if (!typeof(ICompressionProvider).IsAssignableFrom(t))
				throw new Exception("Not ICompressionProvider");

			Compressor = t;
		}
	}

}
