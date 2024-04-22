using System;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;

namespace GoreRemoting.ClientFactory
{
//	/// <summary>
//	/// Provides extension methods to the IServiceCollection API
//	/// </summary>
	public static class ServicesExtensions
	{
		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		public static IHttpClientBuilder AddGoreRemotingClient<T>(this IServiceCollection services) where T : class
		{
			throw new Exception();
				//=> services.AddGrpcClient<T>().ConfigureCodeFirstGrpcClient<T>();

	//		GrpcClientFactoryOptions c;
//			c.
		}

		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		//		public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services,
		//			string name) where T : class
		//			=> services.AddGrpcClient<T>(name).ConfigureCodeFirstGrpcClient<T>();

		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		//		public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services,
		//			string name, Action<GrpcClientFactoryOptions> configureClient) where T : class
		//			=> services.AddGrpcClient<T>(name, configureClient).ConfigureCodeFirstGrpcClient<T>();

		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		//		public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services,
		//			string name, Action<IServiceProvider, GrpcClientFactoryOptions> configureClient) where T : class
		//			=> services.AddGrpcClient<T>(name, configureClient).ConfigureCodeFirstGrpcClient<T>();

		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		//		public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services,
		//			Action<GrpcClientFactoryOptions> configureClient) where T : class
		//			=> services.AddGrpcClient<T>(configureClient).ConfigureCodeFirstGrpcClient<T>();

		//		/// <summary>
		//		/// Registers a provider that can recognize and handle code-first services
		//		/// </summary>
		//		public static IHttpClientBuilder AddCodeFirstGrpcClient<T>(this IServiceCollection services,
		//			Action<IServiceProvider, GrpcClientFactoryOptions> configureClient) where T : class
		//			=> services.AddGrpcClient<T>(configureClient).ConfigureCodeFirstGrpcClient<T>();

		//		/// <summary>
		//		/// Configures the provided client-builder to use code-first GRPC for client creation
		//		/// </summary>
//		public static IHttpClientBuilder ConfigureCodeFirstGrpcClient<T>(this IHttpClientBuilder clientBuilder) where T : class
	//		=> clientBuilder.ConfigureGrpcClientCreator(
		//		(services, callInvoker) => Client.GrpcClientFactory.CreateGrpcService<T>(callInvoker, services.GetService<Configuration.ClientFactory>()));
	}
}
