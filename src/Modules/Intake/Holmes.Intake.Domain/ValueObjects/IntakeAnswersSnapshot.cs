namespace Holmes.Intake.Domain.ValueObjects;

public sealed record IntakeAnswersSnapshot(
    string SchemaVersion,
    string PayloadHash,
    string PayloadCipherText,
    DateTimeOffset UpdatedAt
)
{
    public static IntakeAnswersSnapshot Create(
        string schemaVersion,
        string payloadHash,
        string payloadCipherText,
        DateTimeOffset updatedAt
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadCipherText);

        return new IntakeAnswersSnapshot(schemaVersion, payloadHash, payloadCipherText, updatedAt);
    }
}