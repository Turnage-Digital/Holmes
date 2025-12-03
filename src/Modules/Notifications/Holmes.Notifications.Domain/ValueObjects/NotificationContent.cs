namespace Holmes.Notifications.Domain.ValueObjects;

public sealed record NotificationContent
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? TemplateId { get; init; }
    public IReadOnlyDictionary<string, object> TemplateData { get; init; } = new Dictionary<string, object>();
}
