using BBRepoList.Presentation;

using FluentAssertions;

namespace BBRepoList.Tests.Presentation;

public sealed class PresentationHelpersTests
{
    [Fact(DisplayName = "SplitCompactDisplayName returns dash when display name is empty")]
    [Trait("Category", "Unit")]
    public void SplitCompactDisplayNameWhenDisplayNameIsEmptyReturnsDash()
    {
        var lines = PresentationHelpers.SplitCompactDisplayName("   ");

        lines.Should().Equal("-");
    }

    [Fact(DisplayName = "SplitCompactDisplayName splits two-word name into two lines")]
    [Trait("Category", "Unit")]
    public void SplitCompactDisplayNameWhenNameHasTwoWordsSplitsIntoTwoLines()
    {
        var lines = PresentationHelpers.SplitCompactDisplayName("Jane Doe");

        lines.Should().Equal("Jane", "Doe");
    }

    [Fact(DisplayName = "SplitCompactDisplayName balances longer names across two lines")]
    [Trait("Category", "Unit")]
    public void SplitCompactDisplayNameWhenNameHasManyWordsBalancesAcrossTwoLines()
    {
        var lines = PresentationHelpers.SplitCompactDisplayName("Mary Jane Watson Parker");

        lines.Should().Equal("Mary Jane", "Watson Parker");
    }

    [Fact(DisplayName = "FormatRequestChangesText returns dash when count is zero")]
    [Trait("Category", "Unit")]
    public void FormatRequestChangesTextWhenCountIsZeroReturnsDash()
    {
        var text = PresentationHelpers.FormatRequestChangesText(0);

        text.Should().Be("-");
    }

    [Fact(DisplayName = "FormatRequestChangesText returns formatted count when count is positive")]
    [Trait("Category", "Unit")]
    public void FormatRequestChangesTextWhenCountIsPositiveReturnsFormattedText()
    {
        var text = PresentationHelpers.FormatRequestChangesText(3);

        text.Should().Be("RC (3)");
    }

    [Fact(DisplayName = "FormatRequestChangesText throws when count is negative")]
    [Trait("Category", "Unit")]
    public void FormatRequestChangesTextWhenCountIsNegativeThrowsArgumentOutOfRangeException()
    {
        Action act = () => _ = PresentationHelpers.FormatRequestChangesText(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "FormatApprovalsText returns dash when count is zero")]
    [Trait("Category", "Unit")]
    public void FormatApprovalsTextWhenCountIsZeroReturnsDash()
    {
        var text = PresentationHelpers.FormatApprovalsText(0);

        text.Should().Be("-");
    }

    [Fact(DisplayName = "FormatApprovalsText returns formatted count when count is positive")]
    [Trait("Category", "Unit")]
    public void FormatApprovalsTextWhenCountIsPositiveReturnsFormattedText()
    {
        var text = PresentationHelpers.FormatApprovalsText(2);

        text.Should().Be("AP (2)");
    }

    [Fact(DisplayName = "FormatApprovalsText throws when count is negative")]
    [Trait("Category", "Unit")]
    public void FormatApprovalsTextWhenCountIsNegativeThrowsArgumentOutOfRangeException()
    {
        Action act = () => _ = PresentationHelpers.FormatApprovalsText(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
