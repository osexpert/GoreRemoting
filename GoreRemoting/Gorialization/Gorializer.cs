using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GoreRemoting.RpcMessaging;
using GoreRemoting.Serialization;
using Grpc.Core;
using Grpc.Net.Compression;

namespace GoreRemoting
{
	public interface IGorializer
	{
		//MethodInfo Method { get; set;  }

		void Serialize(GoreBinaryWriter w, Stack<object?> st);
		void Deserialize(GoreBinaryReader r);
		void Deserialize(Stack<object?> st);
	}

	internal class Gorializer
	{

		public static void GoreSerialize(Stream ms, MethodInfo method, IGorializer data, ISerializerAdapter serializer, ICompressionProvider? compressor)
		{
			//using var ms = PooledMemoryStream.GetStream();

			var cs = GetCompressor(compressor, ms) ?? ms;
			try
			{
				var stack = new Stack<object?>();

				var bw = new GoreBinaryWriter(cs);
				data.Serialize(bw, stack);

				if (stack.Any())
				{ 
					var types = GetTypes(data, method, serializer);
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

		public static T GoreDeserialize<T>(Stream ms, MethodInfo method, ISerializerAdapter serializer, ICompressionProvider? compressor) 
			where T : IGorializer, new()
		{
			//			using var ms = new MemoryStream(data);

			var res = new T();
			//res.Method = method;

			var ds = GetDecompressor(compressor, ms) ?? ms;
			try
			{
				var br = new GoreBinaryReader(ds);
				res.Deserialize(br);

				var types = GetTypes(res, method, serializer);

				if (types.Any())
				{
					//var param_s = method.GetParameters();
					object?[] arr = serializer.Deserialize(ds, types);// new Type[] { serializer.ExceptionType });


					res.Deserialize(new Stack<object?>(arr.Reverse()));
				}

				//				object?[] arr = null!;
				//				if (res is MethodResultMessage mrm)
				//				{
				//					if (mrm.ResultType == ResultKind.Exception)
				//					{
				//						arr = serializer.Deserialize(ds, new Type[] { serializer.ExceptionType});
				//					}
				//					else
				//					{
				//						var l = new List<Type>();

				//						Type retType = method.ReturnType;

				//						// we know this from ResultType too...
				//						// we know this from ResultType too... == typeof(ValueTask);
				//						var isVoid = retType == typeof(void) || retType == typeof(Task) || retType == typeof(ValueTask);
				//						if (!isVoid)
				//						{
				//							if (retType.IsGenericType)
				//							{
				//								var gtd = method.ReturnType.GetGenericTypeDefinition();
				//								if (gtd == typeof(ValueTask<>) || gtd == typeof(Task<>))
				//								{
				//									retType = method.ReturnType.GenericTypeArguments.Single();
				//								}
				//							}

				//							l.Add(retType);
				//						}

				//						l.AddRange(mrm.OutArguments
				//							.Select(oa => param_s[oa.Position])
				//							.Select(p => p.IsOutParameterForReal() ? p.ParameterType.GetElementType() : p.ParameterType));

				//						//mrm.OutArguments.Select(oa => method.GetParameters() oa.ParameterName)

				//						arr = serializer.Deserialize(ds, l.ToArray());
				//					}
				//				}
				//				else if (res is MethodCallMessage mcm)
				//				{
				//					arr = serializer.Deserialize(ds, param_s
				////						.Select(p => p.ParameterType)
				//						.Where(p =>
				//						{
				//							if (p.IsOutParameterForReal()
				//								|| typeof(Delegate).IsAssignableFrom(p.ParameterType)
				//								|| typeof(CancellationToken).IsAssignableFrom(p.ParameterType)
				//								)
				//								return false;

				//							return true;
				//						})
				//						.Select(t =>
				//						{
				//							//if (t.IsByRef)
				//							//	t = t.GetElementType();
				//							//else if (typeof(Delegate).IsAssignableFrom(t))
				//							//{
				//							//	//var invokeMethod = t.GetMethod("Invoke");
				//							//	//var retType = invokeMethod.ReturnType;
				//							//	t = typeof(string);//  retType; it does not matter the type. it will always be null anyways, but it can not be void...
				//							//}


				//							return t.ParameterType;
				//						})
				//						.ToArray());
				//				}
				//				else if (res is DelegateCallMessage dcm)
				//				{
				//					// what if delegate without args`? Func vs Action...
				//					//arr = serializer.Deserialize(ds, param_s[dcm.Position].ParameterType.GenericTypeArguments.Skip(1).ToArray());

				//					var delegateType = param_s[dcm.Position].ParameterType;
				//					var invokeMethod = delegateType.GetMethod("Invoke");
				//					arr = serializer.Deserialize(ds, invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());

				//				}
				//				else if (res is DelegateResultMessage drm)
				//				{
				//					if (drm.ReturnKind == DelegateResultType.Exception)
				//					{
				//						arr = serializer.Deserialize(ds, new Type[] { serializer.ExceptionType });
				//					}
				//					else
				//					{
				//						var delegateType = param_s[drm.Position].ParameterType;
				//						var invokeMethod = delegateType.GetMethod("Invoke");

				//						var retType = invokeMethod.ReturnType;

				//						if (invokeMethod.ReturnType.IsGenericType)
				//						{
				//							var gtd = invokeMethod.ReturnType.GetGenericTypeDefinition();
				//							if (gtd == typeof(ValueTask<>) || gtd == typeof(Task<>))
				//							{
				//								retType = invokeMethod.ReturnType.GenericTypeArguments.Single();
				//							}
				//						}

				//						arr = serializer.Deserialize(ds, new Type[] { retType });
				//					}
				//				}
				//				else
				//					throw new Exception();

			}
			finally
			{
				if (ds != ms)
					ds.Dispose();
			}

			return res;
		}

		private static Type[] GetTypes(IGorializer res, MethodInfo method, ISerializerAdapter serializer)
		{
			var param_s = method.GetParameters();

			if (res is MethodResultMessage mrm)
			{
				if (mrm.ResultType == ResultKind.Exception)
				{
					return new Type[] { serializer.ExceptionType };
					//arr = serializer.Deserialize(ds, );
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

					//arr = serializer.Deserialize(ds, l.ToArray());
					return l.ToArray();
				}
			}
			else if (res is MethodCallMessage mcm)
			{
				//arr = serializer.Deserialize(ds, 
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
				return  types;
			}
			else if (res is DelegateCallMessage dcm)
			{
				// what if delegate without args`? Func vs Action...
				//arr = serializer.Deserialize(ds, param_s[dcm.Position].ParameterType.GenericTypeArguments.Skip(1).ToArray());

				var delegateType = param_s[dcm.Position].ParameterType;
				var invokeMethod = delegateType.GetMethod("Invoke");
				//arr = serializer.Deserialize(ds, invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
				return invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();

			}
			else if (res is DelegateResultMessage drm)
			{
				if (drm.ReturnKind == DelegateResultType.Exception)
				{
					//arr = serializer.Deserialize(ds, new Type[] { serializer.ExceptionType });
					return  new Type[] { serializer.ExceptionType };
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

					//arr = serializer.Deserialize(ds, new Type[] { retType });
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

//		public static object?[] DeserializeArguments(ISerializerAdapter ser, MethodInfo method, object?[] parameterValues)
//		{
//			object?[] res = new object[parameterValues.Length];

//			var prms = method.GetParameters();
//			for (int i = 0; i < prms.Length; i++)
//			{
//				var p = prms[i];
//				var v = parameterValues[i];
//				if (v != null)
//				{
////					var t = p.ParameterType;
////					if (t.IsByRef)
////						t = t.GetElementType();
//					res[i] = v;// ser.Deserialize(t, v);
//				}

//				//???????????????????
//				//else
//				//{
//				//	res[i] = v;
//				//}
//			}
//			return res;
//		}
	}

	public class GoreBinaryWriter : BinaryWriter
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryWriter(Stream outp) : base(outp, _utf8NoBom, leaveOpen: true)
		{

		}

		public void WriteVarInt(int i) => base.Write7BitEncodedInt(i);

		public void Write(Guid g) => Write(g.ToByteArray());
	}

	public class GoreBinaryReader : BinaryReader
	{
		static Encoding _utf8NoBom = new UTF8Encoding(false);

		public GoreBinaryReader(Stream inp) : base(inp, _utf8NoBom, leaveOpen: true)
		{
		}

		public int ReadVarInt() => base.Read7BitEncodedInt();

		public Guid ReadGuid() => new Guid(ReadBytes(16));
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
