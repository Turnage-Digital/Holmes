namespace Holmes.SlaClocks.Infrastructure.Sql.Entities;

public class HolidayDb
{
    public int Id { get; set; }
    public string? CustomerId { get; set; } // null = system-wide (federal holidays)
    public DateTime Date { get; set; }
    public string Name { get; set; } = null!;
    public bool IsObserved { get; set; }
}