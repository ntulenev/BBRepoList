using BBRepoList;
using BBRepoList.Presentation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services.AddApplicationServices(builder.Configuration);

using var host = builder.Build();

var app = host.Services.GetRequiredService<ConsoleApp>();
await app.RunAsync(CancellationToken.None).ConfigureAwait(false);

