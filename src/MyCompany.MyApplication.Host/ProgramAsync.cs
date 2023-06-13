#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using ConsoleTables;
using Humanizer;

namespace MyCompany.MyApplication.Host
{
	public class ProgramAsync
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger _logger;
		private readonly IHttpClientFactory _clientFactory;

		//private readonly MyDbContext _dbContext;
		public ProgramAsync(IServiceProvider serviceProvider, ILogger<ProgramAsync> logger, IHttpClientFactory clientFactory) //, MyDbContext dbContext)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
			_clientFactory = clientFactory;
			//_dbContext = dbContext;
		}

		public async Task MainAsync()
		{
			_logger.LogInformation("Async operation started.");

			// var records = await _dbContext.MyTable.OrderByDescending(t => t.Id).Take(10).ToListAsync();
			// var data = records.Select(r => new { r.Id, ModifiedAt = r.ModifiedAt.Humanize() }).ToList();
			// ConsoleTable.From(data).Configure(o => o.NumberAlignment = Alignment.Right).Write(Format.Default);

			CancellationToken cancellationToken = default;
			var requestUri = "https://httpbin.org/ip";
			using HttpClient httpClient = _clientFactory.CreateClient(Program.HttpClientName);
			HttpResponseMessage response = await httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
			response.EnsureSuccessStatusCode();
			var content = await response.Content.ReadAsStringAsync();
			using var jsonDocument = JsonDocument.Parse(content);
			var originValue = jsonDocument.RootElement.GetProperty("origin").GetString();
			AnsiConsole.Write(new FigletText(originValue).LeftJustified().Color(Color.Red));

			await Task.Delay(TimeSpan.FromSeconds(3));
			_logger.LogInformation("Async operation completed.");
		}
	}
}
