using System.Net.Http.Headers;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.API.Helpers;
using BBRepoList.Caching;
using BBRepoList.Configuration;
using BBRepoList.Logic;
using BBRepoList.Presentation;
using BBRepoList.Presentation.Html;
using BBRepoList.Presentation.Pdf;
using BBRepoList.Telemetry;
using BBRepoList.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BBRepoList;

/// <summary>
/// Registers BBRepoList application services in the dependency injection container.
/// </summary>
internal static class ApplicationServiceCollectionExtensions
{
    private const string BITBUCKET_SECTION_NAME = "Bitbucket";

    /// <summary>
    /// Adds BBRepoList configuration, Bitbucket API, reporting, and application services.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddApplicationOptions(configuration);
        _ = services.AddSystemServices();
        _ = services.AddBitbucketTransport();
        _ = services.AddBitbucketApiClients();
        _ = services.AddPullRequestServices();
        _ = services.AddCaching();
        _ = services.AddReportRendering();
        _ = services.AddApplicationWorkflow();

        return services;
    }

    private static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services
            .AddOptions<BitbucketOptions>()
            .Bind(configuration.GetSection(BITBUCKET_SECTION_NAME))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        _ = services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static IServiceCollection AddApplicationWorkflow(this IServiceCollection services)
    {
        _ = services.AddTransient<IRepoService, RepositoryService>();
        _ = services.AddTransient<ConsoleApp>();

        return services;
    }

    private static IServiceCollection AddBitbucketTransport(this IServiceCollection services)
    {
        _ = services.AddHttpClient<IBitbucketTransport, BitbucketTransport>((sp, http) =>
        {
            var settings = sp.GetRequiredService<IOptions<BitbucketOptions>>().Value;
            var authApi = sp.GetRequiredService<IBitbucketAuthApiClient>();

            http.BaseAddress = new Uri(settings.BaseUrl.ToString().TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Authorization = authApi.BuildAuthHeader(settings.AuthEmail, settings.AuthApiToken);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        _ = services.AddSingleton<IBitbucketRetryPolicy, BitbucketRetryPolicy>();
        _ = services.AddSingleton<IBitbucketTelemetryService, BitbucketTelemetryService>();

        return services;
    }

    private static IServiceCollection AddBitbucketApiClients(this IServiceCollection services)
    {
        _ = services.AddTransient<IBitbucketAuthApiClient, BitbucketAuthApiClient>();
        _ = services.AddTransient<IBitbucketRepoApiClient, BitbucketRepoApiClient>();
        _ = services.AddTransient<IBitbucketJsonParser, BitbucketJsonParser>();
        _ = services.AddTransient<IBitbucketPRApiClient, BitbucketPRApiClient>();

        return services;
    }

    private static IServiceCollection AddPullRequestServices(this IServiceCollection services)
    {
        _ = services.AddTransient<IPullRequestActivityAnalyzer, PullRequestActivityAnalyzer>();
        _ = services.AddTransient<IBitbucketPullRequestActivityLoader, BitbucketPullRequestActivityLoader>();
        _ = services.AddTransient<IPullRequestFingerprintBuilder, PullRequestFingerprintBuilder>();
        _ = services.AddTransient<IPullRequestSnapshotMapper, PullRequestSnapshotMapper>();

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        _ = services.AddSingleton<IPullRequestDetailsCache, FilePullRequestDetailsCache>();
        _ = services.AddSingleton<IPullRequestDetailsCacheService, PullRequestDetailsCacheService>();

        return services;
    }

    private static IServiceCollection AddReportRendering(this IServiceCollection services)
    {
        _ = services.AddTransient<IRepositoryReportDataFactory, RepositoryReportDataFactory>();
        _ = services.AddTransient<IConsoleReportRenderer, ConsoleReportRenderer>();
        _ = services.AddTransient<IHtmlContentComposer, HtmlContentComposer>();
        _ = services.AddTransient<IHtmlReportFileStore, HtmlReportFileStore>();
        _ = services.AddTransient<IHtmlReportLauncher, HtmlReportLauncher>();
        _ = services.AddTransient<IHtmlReportRenderer, HtmlReportRenderer>();
        _ = services.AddTransient<IPdfContentComposer, PdfContentComposer>();
        _ = services.AddTransient<IPdfReportFileStore, PdfReportFileStore>();
        _ = services.AddTransient<IPdfReportRenderer, QuestPdfReportRenderer>();

        return services;
    }
}
