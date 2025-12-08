using Holmes.Core.Domain.ValueObjects;

namespace Holmes.SlaClocks.Application.Services;

/// <summary>
///     Service for calculating business day deadlines, accounting for weekends and holidays.
/// </summary>
public interface IBusinessCalendarService
{
    /// <summary>
    ///     Calculate deadline by adding business days to start date.
    ///     Excludes weekends and holidays for the given customer.
    /// </summary>
    DateTimeOffset AddBusinessDays(DateTimeOffset start, int businessDays, UlidId customerId);

    /// <summary>
    ///     Calculate when the at-risk threshold occurs between start and deadline.
    /// </summary>
    DateTimeOffset CalculateAtRiskThreshold(
        DateTimeOffset start,
        DateTimeOffset deadline,
        decimal thresholdPercent
    );

    /// <summary>
    ///     Check if a given date is a business day for this customer.
    /// </summary>
    bool IsBusinessDay(DateTimeOffset date, UlidId customerId);
}