using BBRepoList.Models;
using BBRepoList.Presentation.Html;

using FluentAssertions;

namespace BBRepoList.Tests.Presentation.Html;

public sealed class HtmlContentComposerTests
{
    [Fact(DisplayName = "Compose renders empty state using embedded template")]
    [Trait("Category", "Unit")]
    public void ComposeWhenThereAreNoPullRequestDetailsRendersEmptyState()
    {
        var composer = new HtmlContentComposer();
        var reportData = new RepositoryPdfReportData(
            "workspace",
            null,
            12,
            true,
            4,
            10,
            new DateTimeOffset(2026, 3, 13, 10, 30, 0, TimeSpan.Zero),
            [],
            []);

        var html = composer.Compose(reportData);

        html.Should().Contain("<title>Open PR Details - workspace</title>");
        html.Should().Contain("No open pull request details were collected for this run.");
        html.Should().NotContain("__ROWS__");
        html.Should().NotContain("__WORKSPACE_TITLE__");
    }

    [Fact(DisplayName = "Compose renders pull request rows from embedded row template")]
    [Trait("Category", "Unit")]
    public void ComposeWhenThereArePullRequestDetailsRendersSummaryAndRows()
    {
        var composer = new HtmlContentComposer();
        var repository = new Repository("Repo <One>", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), null, "repo-one");
        var detail = new PullRequestDetail(
            repository,
            101,
            "Fix <bug>",
            new DateTimeOffset(2026, 3, 13, 8, 0, 0, TimeSpan.Zero),
            authorId: null,
            firstNonAuthorActivityOn: new DateTimeOffset(2026, 3, 13, 9, 0, 0, TimeSpan.Zero),
            lastActivityOn: new DateTimeOffset(2026, 3, 13, 9, 30, 0, TimeSpan.Zero),
            hasCurrentUserDiscussion: true,
            descriptionText: "A detailed description",
            requestChangesCount: 2,
            hasCurrentUserRequestChanges: true,
            approvalsCount: 1,
            hasCurrentUserApproval: true);
        var reportData = new RepositoryPdfReportData(
            "workspace",
            "<hotfix>",
            12,
            true,
            4,
            10,
            new DateTimeOffset(2026, 3, 13, 10, 30, 0, TimeSpan.Zero),
            [repository],
            [detail]);

        var html = composer.Compose(reportData);

        html.Should().Contain("&lt;hotfix&gt;");
        html.Should().Contain("Repo &lt;One&gt;");
        html.Should().Contain("Fix &lt;bug&gt;");
        html.Should().Contain("RC (2)");
        html.Should().Contain("AP (1)");
        html.Should().Contain("https://bitbucket.org/workspace/repo-one/pull-requests/101");
        html.Should().Contain("&#128172;");
        html.Should().Contain("&#10060;");
        html.Should().Contain("&#9989;");
        html.Should().NotContain("__PULL_REQUEST_ID__");
    }
}
