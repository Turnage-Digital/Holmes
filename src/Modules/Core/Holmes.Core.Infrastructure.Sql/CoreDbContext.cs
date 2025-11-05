using Holmes.Core.Infrastructure.Sql.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public class CoreDbContext(DbContextOptions<CoreDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<OutboxMessageDb> OutboxMessages { get; set; } = null!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<OutboxMessageDb>();
        entity.ToTable("OutboxMessages");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Type).HasMaxLength(300).IsRequired();
        entity.Property(x => x.PayloadJson).HasColumnType("LONGTEXT").IsRequired();
        entity.Property(x => x.CreatedOn).IsRequired();
        entity.Property(x => x.AvailableAfter);
        entity.Property(x => x.Attempts).HasDefaultValue(0).IsRequired();
        entity.Property(x => x.LastError).HasMaxLength(2000);
        entity.HasIndex(x => x.ProcessedOn);
        entity.HasIndex(x => x.AvailableAfter);
        entity.HasIndex(x => new { x.CreatedOn, x.ProcessedOn });
    }
}