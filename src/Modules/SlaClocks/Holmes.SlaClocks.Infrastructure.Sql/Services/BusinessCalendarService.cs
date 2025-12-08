using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Services;

namespace Holmes.SlaClocks.Infrastructure.Sql.Services;

public sealed class BusinessCalendarService(SlaClockDbContext context) : IBusinessCalendarService
{
    // US Federal Holidays (observed dates for 2024-2026)
    private static readonly HashSet<DateTime> FederalHolidays =
    [
        // 2024
        new(2024, 1, 1), // New Year's Day
        new(2024, 1, 15), // MLK Day
        new(2024, 2, 19), // Presidents' Day
        new(2024, 5, 27), // Memorial Day
        new(2024, 6, 19), // Juneteenth
        new(2024, 7, 4), // Independence Day
        new(2024, 9, 2), // Labor Day
        new(2024, 10, 14), // Columbus Day
        new(2024, 11, 11), // Veterans Day
        new(2024, 11, 28), // Thanksgiving
        new(2024, 12, 25), // Christmas

        // 2025
        new(2025, 1, 1), // New Year's Day
        new(2025, 1, 20), // MLK Day
        new(2025, 2, 17), // Presidents' Day
        new(2025, 5, 26), // Memorial Day
        new(2025, 6, 19), // Juneteenth
        new(2025, 7, 4), // Independence Day
        new(2025, 9, 1), // Labor Day
        new(2025, 10, 13), // Columbus Day
        new(2025, 11, 11), // Veterans Day
        new(2025, 11, 27), // Thanksgiving
        new(2025, 12, 25), // Christmas

        // 2026
        new(2026, 1, 1), // New Year's Day
        new(2026, 1, 19), // MLK Day
        new(2026, 2, 16), // Presidents' Day
        new(2026, 5, 25), // Memorial Day
        new(2026, 6, 19), // Juneteenth
        new(2026, 7, 3), // Independence Day (observed)
        new(2026, 9, 7), // Labor Day
        new(2026, 10, 12), // Columbus Day
        new(2026, 11, 11), // Veterans Day
        new(2026, 11, 26), // Thanksgiving
        new(2026, 12, 25) // Christmas
    ];

    public DateTimeOffset AddBusinessDays(DateTimeOffset start, int businessDays, UlidId customerId)
    {
        var current = start;
        var daysAdded = 0;

        // Get customer-specific holidays
        var customerHolidays = GetCustomerHolidays(customerId);

        while (daysAdded < businessDays)
        {
            current = current.AddDays(1);

            if (IsBusinessDayInternal(current.Date, customerHolidays))
            {
                daysAdded++;
            }
        }

        return current;
    }

    public DateTimeOffset CalculateAtRiskThreshold(
        DateTimeOffset start,
        DateTimeOffset deadline,
        decimal thresholdPercent
    )
    {
        var totalTime = deadline - start;
        var thresholdTime = TimeSpan.FromTicks((long)(totalTime.Ticks * (double)thresholdPercent));
        return start + thresholdTime;
    }

    public bool IsBusinessDay(DateTimeOffset date, UlidId customerId)
    {
        var customerHolidays = GetCustomerHolidays(customerId);
        return IsBusinessDayInternal(date.Date, customerHolidays);
    }

    private static bool IsBusinessDayInternal(DateTime date, HashSet<DateTime> customerHolidays)
    {
        // Weekend check
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        // Federal holiday check
        if (FederalHolidays.Contains(date.Date))
        {
            return false;
        }

        // Customer-specific holiday check
        if (customerHolidays.Contains(date.Date))
        {
            return false;
        }

        return true;
    }

    private HashSet<DateTime> GetCustomerHolidays(UlidId customerId)
    {
        // Query customer-specific holidays from database
        var customerIdStr = customerId.ToString();
        var holidays = context.Holidays
            .Where(h => h.CustomerId == customerIdStr || h.CustomerId == null)
            .Select(h => h.Date)
            .ToHashSet();

        return holidays;
    }
}