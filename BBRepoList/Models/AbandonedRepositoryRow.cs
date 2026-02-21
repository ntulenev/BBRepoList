namespace BBRepoList.Models;

/// <summary>
/// Row model for abandoned repositories presentation.
/// </summary>
public sealed class AbandonedRepositoryRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbandonedRepositoryRow"/> class.
    /// </summary>
    /// <param name="repository">Repository data.</param>
    /// <param name="lastActivityOn">Last activity date.</param>
    /// <param name="monthsWithoutActivity">Number of full inactive months.</param>
    public AbandonedRepositoryRow(Repository repository, DateTimeOffset lastActivityOn, int monthsWithoutActivity)
    {
        ArgumentNullException.ThrowIfNull(repository);

        Repository = repository;
        LastActivityOn = lastActivityOn;
        MonthsWithoutActivity = monthsWithoutActivity;
    }

    /// <summary>
    /// Repository data.
    /// </summary>
    public Repository Repository { get; }

    /// <summary>
    /// Last activity date.
    /// </summary>
    public DateTimeOffset LastActivityOn { get; }

    /// <summary>
    /// Number of full inactive months.
    /// </summary>
    public int MonthsWithoutActivity { get; }
}
