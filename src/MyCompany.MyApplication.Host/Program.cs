#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace MyCompany.MyApplication.Host
{
	public sealed class Program
	{
		internal const string HttpClientName = "MyHttpClient";

		public static async Task Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			
			bool configurationDebug = false;
#if (DEBUG)
			configurationDebug = true;
#endif
			Console.WriteLine($"RUN-TIME\n{new string('=', 20)}\n{Assembly.GetExecutingAssembly().GetName().Version}\nCONFIGURATION: {(configurationDebug ? "Debug" : "Release")}\nASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}\nDOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}\nDOTNET_RUNNING_IN_CONTAINER: {Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")}\nMACHINENAME: {Environment.MachineName}");

			var defaultRetryPolicy = Policy
				.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.InternalServerError)
				.OrResult(r => r.StatusCode == HttpStatusCode.BadGateway)
				.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
					async (exception, timeSpan, retryCount, context) =>
					{
						Console.WriteLine($"An error occurred: {exception.Result.StatusCode}. Retrying in {timeSpan}...");
						await Task.CompletedTask;
					}
				);

			using (IHost host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration(appConfiguration =>
				{
					appConfiguration.AddJsonFile("appsettings.json");
					appConfiguration.AddUserSecrets<Program>();
				})
				.ConfigureServices((context, services) =>
				{
					services.AddHttpClient(HttpClientName).AddPolicyHandler(defaultRetryPolicy);
					//services.AddDbContext<MyDbContext>(options => { options.UseSqlServer(context.Configuration.GetConnectionString("MsSqlConnection")); });
					services.AddScoped<ProgramAsync>();
				})
				.ConfigureLogging((context, configuration) =>
				{
					configuration.ClearProviders();
					configuration.AddConfiguration(context.Configuration.GetSection("Logging"));
					configuration.AddConsole();
				})
				.Build()
				)
			{
				using (IServiceScope scope = host.Services.CreateScope())
				{
					ProgramAsync pa = scope.ServiceProvider.GetRequiredService<ProgramAsync>();
					pa.MainAsync().Wait();
				}
			}

			Console.WriteLine("Exiting...");
		}
	}
}
