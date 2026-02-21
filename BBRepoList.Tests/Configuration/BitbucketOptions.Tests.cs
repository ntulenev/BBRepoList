using FluentAssertions;

using System.ComponentModel.DataAnnotations;

using BBRepoList.Configuration;

namespace BBRepoList.Tests.Configuration;

public sealed class BitbucketOptionsTests
{
    [Fact(DisplayName = "Validation succeeds when options are valid")]
    [Trait("Category", "Unit")]
    public void ValidateWhenOptionsAreValidReturnsNoErrors()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().BeEmpty();
        options.AbandonedMonthsThreshold.Should().Be(12);
    }

    [Fact(DisplayName = "Validation fails when base url is missing")]
    [Trait("Category", "Unit")]
    public void ValidateWhenBaseUrlIsMissingReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = null!,
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("BaseUrl"));
    }

    [Fact(DisplayName = "Validation fails when workspace is empty")]
    [Trait("Category", "Unit")]
    public void ValidateWhenWorkspaceIsEmptyReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = string.Empty,
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("Workspace"));
    }

    [Fact(DisplayName = "Validation fails when auth email is empty")]
    [Trait("Category", "Unit")]
    public void ValidateWhenAuthEmailIsEmptyReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = " ",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("AuthEmail"));
    }

    [Fact(DisplayName = "Validation fails when auth api token is empty")]
    [Trait("Category", "Unit")]
    public void ValidateWhenAuthApiTokenIsEmptyReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = string.Empty,
            PageLen = 25,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("AuthApiToken"));
    }

    [Fact(DisplayName = "Validation fails when page length is less than one")]
    [Trait("Category", "Unit")]
    public void ValidateWhenPageLenIsBelowRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 0,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("PageLen"));
    }

    [Fact(DisplayName = "Validation fails when page length is greater than one hundred")]
    [Trait("Category", "Unit")]
    public void ValidateWhenPageLenIsAboveRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 101,
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("PageLen"));
    }

    [Fact(DisplayName = "Validation fails when retry count is below zero")]
    [Trait("Category", "Unit")]
    public void ValidateWhenRetryCountIsBelowRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = -1
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("RetryCount"));
    }

    [Fact(DisplayName = "Validation fails when retry count is greater than ten")]
    [Trait("Category", "Unit")]
    public void ValidateWhenRetryCountIsAboveRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 11
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("RetryCount"));
    }

    [Fact(DisplayName = "Validation fails when abandoned threshold is below one")]
    [Trait("Category", "Unit")]
    public void ValidateWhenAbandonedMonthsThresholdIsBelowRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            AbandonedMonthsThreshold = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("AbandonedMonthsThreshold"));
    }

    [Fact(DisplayName = "Validation fails when abandoned threshold is above one hundred twenty")]
    [Trait("Category", "Unit")]
    public void ValidateWhenAbandonedMonthsThresholdIsAboveRangeReturnsError()
    {
        // Arrange
        var options = new BitbucketOptions
        {
            BaseUrl = new Uri("https://api.bitbucket.org/2.0/", UriKind.Absolute),
            Workspace = "workspace",
            AuthEmail = "user@example.test",
            AuthApiToken = "token",
            PageLen = 25,
            RetryCount = 0,
            AbandonedMonthsThreshold = 121
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("AbandonedMonthsThreshold"));
    }

    private static List<ValidationResult> Validate(BitbucketOptions options)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);

        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        return results;
    }
}
