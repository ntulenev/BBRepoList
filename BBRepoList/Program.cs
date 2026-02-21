using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Presentation;
using BBRepoList.Presentation.Pdf;
using BBRepoList.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System.Net.Http.Headers;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services
    .AddOptions<BitbucketOptions>()
    .Bind(builder.Configuration.GetSection("Bitbucket"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IBitbucketTransport, BitbucketTransport>((sp, http) =>
{
    var settings = sp.GetRequiredService<IOptions<BitbucketOptions>>().Value;
    http.BaseAddress = new Uri(settings.BaseUrl.ToString().TrimEnd('/') + "/");
    http.DefaultRequestHeaders.Authorization = BitbucketApiClient.BuildAuthHeader(settings.AuthEmail, settings.AuthApiToken);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<IBitbucketRetryPolicy, BitbucketRetryPolicy>();
builder.Services.AddTransient<IBitbucketApiClient, BitbucketApiClient>();
builder.Services.AddTransient<IRepoService, RepositoryService>();
builder.Services.AddTransient<IPdfContentComposer, PdfContentComposer>();
builder.Services.AddTransient<IPdfReportFileStore, PdfReportFileStore>();
builder.Services.AddTransient<IPdfReportRenderer, QuestPdfReportRenderer>();
builder.Services.AddTransient<ConsoleApp>();

using var host = builder.Build();

var app = host.Services.GetRequiredService<ConsoleApp>();
await app.RunAsync(CancellationToken.None).ConfigureAwait(false);
