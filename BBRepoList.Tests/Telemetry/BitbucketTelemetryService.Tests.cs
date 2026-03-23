using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Telemetry;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace BBRepoList.Tests.Telemetry;

public sealed class BitbucketTelemetryServiceTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketTelemetryService(options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "TrackRequest aggregates requests by normalized Bitbucket API")]
    [Trait("Category", "Unit")]
    public void TrackRequestWhenTelemetryEnabledAggregatesRequests()
    {
        // Arrange
        var service = new BitbucketTelemetryService(Options.Create(CreateOptions(enabled: true)));

        // Act
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/user", UriKind.Absolute));
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/repositories/workspace?q=name", UriKind.Absolute));
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/repositories/workspace/repo-one/pullrequests?state=OPEN&pagelen=1&fields=size", UriKind.Absolute));
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/repositories/workspace/repo-two/pullrequests?state=OPEN&pagelen=50&fields=values.id%2Cvalues.title", UriKind.Absolute));
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/repositories/workspace/repo-two/pullrequests/101/activity?pagelen=50", UriKind.Absolute));
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/repositories/workspace/repo-three/pullrequests/102/activity?pagelen=50", UriKind.Absolute));

        // Assert
        service.GetSnapshot().Should().BeEquivalentTo(new BitbucketTelemetrySnapshot(
            true,
            6,
            [
                new BitbucketApiRequestStatistic("GET /repositories/{workspace}/{repository}/pullrequests/{pullRequestId}/activity", 2),
                new BitbucketApiRequestStatistic("GET /repositories/{workspace}", 1),
                new BitbucketApiRequestStatistic("GET /repositories/{workspace}/{repository}/pullrequests", 1),
                new BitbucketApiRequestStatistic("GET /repositories/{workspace}/{repository}/pullrequests (count)", 1),
                new BitbucketApiRequestStatistic("GET /user", 1)
            ]));
    }

    [Fact(DisplayName = "TrackRequest does nothing when telemetry is disabled")]
    [Trait("Category", "Unit")]
    public void TrackRequestWhenTelemetryDisabledReturnsEmptySnapshot()
    {
        // Arrange
        var service = new BitbucketTelemetryService(Options.Create(CreateOptions(enabled: false)));

        // Act
        service.TrackRequest(new Uri("https://api.bitbucket.org/2.0/user", UriKind.Absolute));

        // Assert
        var snapshot = service.GetSnapshot();
        snapshot.IsEnabled.Should().BeFalse();
        snapshot.TotalRequests.Should().Be(0);
        snapshot.RequestStatistics.Should().BeEmpty();
    }

    private static BitbucketOptions CreateOptions(bool enabled)
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            Telemetry = new BitbucketTelemetryOptions
            {
                Enabled = enabled
            }
        };
    }
}
