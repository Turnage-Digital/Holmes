namespace Holmes.IntakeSessions.Infrastructure.Sql.Entities;

public class ConsentArtifactDb
{
    public string ArtifactId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string SubjectId { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long Length { get; set; }
    public string Hash { get; set; } = null!;
    public string HashAlgorithm { get; set; } = null!;
    public string SchemaVersion { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] Payload { get; set; } = [];
    public string MetadataJson { get; set; } = "{}";
}