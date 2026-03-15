using System.Text.Json;

using BBRepoList.Abstractions;
using BBRepoList.API;
using BBRepoList.API.Helpers;
using BBRepoList.Configuration;
using BBRepoList.Models;
using BBRepoList.Transport;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace BBRepoList.Tests.API;

public sealed class BitbucketPRApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IBitbucketTransport transport = null!;
        var parser = new BitbucketJsonParser();
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport, parser, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when json parser is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenJsonParserIsNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        IBitbucketJsonParser parser = null!;
        var options = Options.Create(CreateOptions());

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport.Object, parser, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        var parser = new BitbucketJsonParser();
        IOptions<BitbucketOptions> options = null!;

        // Act
        Action act = () => _ = new BitbucketPRApiClient(transport.Object, parser, options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync enriches open pull requests count when slug is present")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenSlugIsPresentLoadsOpenPullRequestsCount()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestSummaryUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=1&fields=size";

        var repository = new Repository("Repo-1", null, null, "repo-1");
        var pullRequestSummaryDto = new PullRequestPageSummaryDto(9);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestSummaryUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(pullRequestSummaryDto);

        var client = new BitbucketPRApiClient(transport.Object, new BitbucketJsonParser(), Options.Create(CreateOptions()));

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        repository.Name.Should().Be("Repo-1");
        repository.OpenPullRequestsCount.Should().Be(9);
    }

    [Fact(DisplayName = "PopulateOpenPullRequestCountAsync keeps repository when pull requests count lookup fails")]
    [Trait("Category", "Unit")]
    public async Task PopulateOpenPullRequestCountAsyncWhenPullRequestsLookupFailsKeepsRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestSummaryUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=1&fields=size";
        var repository = new Repository("Repo-1", null, null, "repo-1");

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageSummaryDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestSummaryUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketPRApiClient(transport.Object, new BitbucketJsonParser(), Options.Create(CreateOptions()));

        // Act
        await client.PopulateOpenPullRequestCountAsync(repository, cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        repository.Name.Should().Be("Repo-1");
        repository.OpenPullRequestsCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns mapped open pull request details")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenResponsesAreValidReturnsDetails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestsUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=25";
        var firstPullRequestUrl = "repositories/workspace/repo-1/pullrequests/101";
        var firstActivityUrl = "repositories/workspace/repo-1/pullrequests/101/activity?pagelen=25";
        var secondPullRequestUrl = "repositories/workspace/repo-1/pullrequests/102";
        var secondActivityUrl = "repositories/workspace/repo-1/pullrequests/102/activity?pagelen=25";

        var pullRequestsDto = new PullRequestPageDto(
        [
            new PullRequestDto(
                Id: 101,
                Title: "Feature A",
                CreatedOn: new DateTimeOffset(2026, 2, 24, 8, 0, 0, TimeSpan.Zero),
                Description: "Detailed description for Feature A",
                Author: new PullRequestAuthorDto("{author-1}", "Author 1")),
            new PullRequestDto(
                Id: 102,
                Title: "Feature B",
                CreatedOn: new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.Zero),
                Summary: new PullRequestSummaryDto("Summary fallback for Feature B"),
                Author: new PullRequestAuthorDto("{author-2}", "Author 2"))
        ],
            null);

        var firstActivityDto = new PullRequestActivityPageDto(
        [
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["approval"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{reviewer-1}" },
                          "date": "2026-02-24T10:00:00+00:00"
                        }
                        """)
                }
            },
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["comment"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{current-user}" },
                          "created_on": "2026-02-24T11:00:00+00:00"
                        }
                        """)
                }
            }
        ],
            null);

        var firstPullRequestDto = new PullRequestDto(
            Id: 101,
            Participants:
            [
                new PullRequestParticipantDto(
                    User: new PullRequestAuthorDto("{reviewer-1}", "Reviewer 1"),
                    State: "changes_requested"),
                new PullRequestParticipantDto(
                    User: new PullRequestAuthorDto("{current-user}", "Current User"),
                    State: "changes requested"),
                new PullRequestParticipantDto(
                    User: new PullRequestAuthorDto("{approver-1}", "Approver 1"),
                    State: "approved"),
                new PullRequestParticipantDto(
                    User: new PullRequestAuthorDto("{current-user}", "Current User"),
                    Approved: true)
            ]);

        var secondActivityDto = new PullRequestActivityPageDto(
        [
            new PullRequestActivityDto
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["comment"] = ParseJsonElement(
                        """
                        {
                          "user": { "uuid": "{author-2}" },
                          "created_on": "2026-02-24T10:30:00+00:00"
                        }
                        """)
                }
            }
        ],
            null);

        var secondPullRequestDto = new PullRequestDto(
            Id: 102,
            Participants:
            [
                new PullRequestParticipantDto(
                    User: new PullRequestAuthorDto("{reviewer-2}", "Reviewer 2"),
                    State: "approved")
            ]);

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestsUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(pullRequestsDto);
        transport
            .Setup(t => t.GetAsync<PullRequestDto>(
                It.Is<Uri>(u => u.ToString() == firstPullRequestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstPullRequestDto);
        transport
            .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == firstActivityUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstActivityDto);
        transport
            .Setup(t => t.GetAsync<PullRequestDto>(
                It.Is<Uri>(u => u.ToString() == secondPullRequestUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondPullRequestDto);
        transport
            .Setup(t => t.GetAsync<PullRequestActivityPageDto>(
                It.Is<Uri>(u => u.ToString() == secondActivityUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondActivityDto);

        var client = new BitbucketPRApiClient(transport.Object, new BitbucketJsonParser(), Options.Create(CreateOptions()));
        var repository = new Repository(
            "Repo-1",
            new DateTimeOffset(2023, 1, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            "repo-1");
        repository.UpdateOpenPullRequestsCount(2);

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(
            repository,
            new BitbucketId("{current-user}"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(5);
        details.Should().HaveCount(2);
        details.Select(d => d.PullRequestId).Should().ContainInOrder(101, 102);
        details[0].FirstNonAuthorActivityOn.Should().Be(new DateTimeOffset(2026, 2, 24, 10, 0, 0, TimeSpan.Zero));
        details[0].LastActivityOn.Should().Be(new DateTimeOffset(2026, 2, 24, 11, 0, 0, TimeSpan.Zero));
        details[0].HasCurrentUserDiscussion.Should().BeTrue();
        details[0].AuthorDisplayName.Should().Be("Author 1");
        details[0].CommentsCount.Should().Be(1);
        details[0].RequestChangesCount.Should().Be(2);
        details[0].HasCurrentUserRequestChanges.Should().BeTrue();
        details[0].ApprovalsCount.Should().Be(2);
        details[0].HasCurrentUserApproval.Should().BeTrue();
        details[0].TimeToFirstResponse.Should().Be(TimeSpan.FromHours(2));
        details[0].DescriptionText.Should().Be("Detailed description for Feature A");
        details[1].FirstNonAuthorActivityOn.Should().BeNull();
        details[1].LastActivityOn.Should().Be(new DateTimeOffset(2026, 2, 24, 10, 30, 0, TimeSpan.Zero));
        details[1].HasCurrentUserDiscussion.Should().BeFalse();
        details[1].AuthorDisplayName.Should().Be("Author 2");
        details[1].CommentsCount.Should().Be(1);
        details[1].RequestChangesCount.Should().Be(0);
        details[1].HasCurrentUserRequestChanges.Should().BeFalse();
        details[1].ApprovalsCount.Should().Be(1);
        details[1].HasCurrentUserApproval.Should().BeFalse();
        details[1].DescriptionText.Should().Be("Summary fallback for Feature B");
    }

    [Fact(DisplayName = "GetOpenPullRequestDetailsAsync returns empty list when lookup fails")]
    [Trait("Category", "Unit")]
    public async Task GetOpenPullRequestDetailsAsyncWhenLookupFailsReturnsEmptyList()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var pullRequestsUrl = "repositories/workspace/repo-1/pullrequests?state=OPEN&pagelen=25";

        var transport = new Mock<IBitbucketTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<PullRequestPageDto>(
                It.Is<Uri>(u => u.ToString() == pullRequestsUrl),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => sendCalls++)
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = new BitbucketPRApiClient(transport.Object, new BitbucketJsonParser(), Options.Create(CreateOptions()));
        var repository = new Repository("Repo-1", null, null, "repo-1");
        repository.UpdateOpenPullRequestsCount(1);

        // Act
        var details = await client.GetOpenPullRequestDetailsAsync(
            repository,
            new BitbucketId("{current-user}"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        details.Should().BeEmpty();
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static BitbucketOptions CreateOptions()
    {
        return new BitbucketOptions
        {
            BaseUrl = new Uri("https://example.test/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };
    }
}

