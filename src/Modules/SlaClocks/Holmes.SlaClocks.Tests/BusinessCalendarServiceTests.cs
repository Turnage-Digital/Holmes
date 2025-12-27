using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Infrastructure.Sql;
using Holmes.SlaClocks.Infrastructure.Sql.Services;
using Microsoft.EntityFrameworkCore;

namespace Holmes.SlaClocks.Tests;

public sealed class BusinessCalendarServiceTests
{
    private SlaClocksDbContext _context = null!;
    private UlidId _customerId;
    private BusinessCalendarService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<SlaClocksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SlaClocksDbContext(options);
        _service = new BusinessCalendarService(_context);
        _customerId = UlidId.NewUlid();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void AddBusinessDays_SkipsWeekends()
    {
        // Friday
        var friday = new DateTimeOffset(2025, 1, 3, 9, 0, 0, TimeSpan.Zero);

        var result = _service.AddBusinessDays(friday, 1, _customerId);

        // Should be Monday
        Assert.That(result.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(result.Date, Is.EqualTo(new DateTime(2025, 1, 6)));
    }

    [Test]
    public void AddBusinessDays_SkipsHolidays()
    {
        // Day before New Year's Day (observed)
        var dec31 = new DateTimeOffset(2024, 12, 31, 9, 0, 0, TimeSpan.Zero);

        var result = _service.AddBusinessDays(dec31, 1, _customerId);

        // Should skip Jan 1 (New Year's Day) and land on Jan 2
        Assert.That(result.Date, Is.EqualTo(new DateTime(2025, 1, 2)));
    }

    [Test]
    public void AddBusinessDays_HandlesMultipleDays()
    {
        // Monday
        var monday = new DateTimeOffset(2025, 1, 6, 9, 0, 0, TimeSpan.Zero);

        var result = _service.AddBusinessDays(monday, 5, _customerId);

        // Should be next Monday (skipping weekend)
        Assert.That(result.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(result.Date, Is.EqualTo(new DateTime(2025, 1, 13)));
    }

    [Test]
    public void AddBusinessDays_ZeroDays_ReturnsSameDay()
    {
        var monday = new DateTimeOffset(2025, 1, 6, 9, 0, 0, TimeSpan.Zero);

        var result = _service.AddBusinessDays(monday, 0, _customerId);

        Assert.That(result, Is.EqualTo(monday));
    }

    [Test]
    public void IsBusinessDay_ReturnsFalse_ForWeekend()
    {
        var saturday = new DateTimeOffset(2025, 1, 4, 9, 0, 0, TimeSpan.Zero);
        var sunday = new DateTimeOffset(2025, 1, 5, 9, 0, 0, TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(_service.IsBusinessDay(saturday, _customerId), Is.False);
            Assert.That(_service.IsBusinessDay(sunday, _customerId), Is.False);
        });
    }

    [Test]
    public void IsBusinessDay_ReturnsFalse_ForHoliday()
    {
        // Christmas 2025 falls on Thursday
        var christmas = new DateTimeOffset(2025, 12, 25, 9, 0, 0, TimeSpan.Zero);

        Assert.That(_service.IsBusinessDay(christmas, _customerId), Is.False);
    }

    [Test]
    public void IsBusinessDay_ReturnsTrue_ForRegularWeekday()
    {
        var tuesday = new DateTimeOffset(2025, 1, 7, 9, 0, 0, TimeSpan.Zero);

        Assert.That(_service.IsBusinessDay(tuesday, _customerId), Is.True);
    }

    [Test]
    public void CalculateAtRiskThreshold_Returns80PercentPoint()
    {
        var start = new DateTimeOffset(2025, 1, 6, 9, 0, 0, TimeSpan.Zero);
        var deadline = start.AddDays(10);

        var result = _service.CalculateAtRiskThreshold(start, deadline, 0.80m);

        // 80% of 10 days = 8 days from start
        var expected = start.AddDays(8);
        Assert.That(result, Is.EqualTo(expected));
    }
}