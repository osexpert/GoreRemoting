using Grpc.Core;
using GoreRemoting.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace GoreRemoting
{
    /// <summary>
    /// Provides configuration settings for a CoreRemoting client instance.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Set to be notified before a method call
        /// </summary>
        public ActionRef<Type, MethodInfo, Metadata, ISerializerAdapter> BeforeMethodCall { get; set; }

		public delegate void ActionRef<T1, T2, T3, T4>(T1 a, T2 b, T3 c, ref T4 d);

		//static ISerializerAdapter _binary_formatter = new BinarySerializerAdapter();

        public ISerializerAdapter DefaultSerializer { get; set; } //= _binary_formatter;

	}
}
