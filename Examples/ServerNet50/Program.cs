using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServerNet60
{
	internal class Program
	{
		/// <summary>
		/// grpc-dotnet/perf/benchmarkapps/QpsWorker
		/// https://github.com/grpc/grpc-dotnet/pull/1617/files#diff-4cde0178bebee2be11d6a73b69dfaffe4156abd06952f82072d376bea5dcecd0
		/// Found here: https://github.com/grpc/grpc-dotnet/issues/1628#issuecomment-1063465524
		/// 
		/// Somthing about dynamic services: https://github.com/grpc/grpc-dotnet/issues/1690
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		// Additional configuration is required to successfully run gRPC on macOS.
		// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.ConfigureLogging((f) =>
					{

						f.ClearProviders();
					});
					webBuilder.UseStartup<Startup>();
					webBuilder.ConfigureKestrel(kestrel =>
					{
						//              var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
						//                var certPath = Path.Combine(basePath!, "Certs", "server1.pfx");

						kestrel.ListenAnyIP(5000, listenOptions =>
						{
							listenOptions.Protocols = HttpProtocols.Http2;

							// Contents of "securityParams" are basically ignored.
							// Instead the server is setup with the default test cert.
							//if (config.SecurityParams != null)
							//{
							//    listenOptions.UseHttps(certPath, "1111");
							//}
						}
						);

						// Other gRPC servers don't include a server header
						kestrel.AddServerHeader = false;
					});
				});




	}

}