using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Chapar.API.DTOs;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options
) : DbContext(options)
  , IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAPIKey> TenantAPIKeys => Set<TenantAPIKey>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<MessageRequest> MessageRequests => Set<MessageRequest>();
    public DbSet<OTPChallenge> OTPs => Set<OTPChallenge>();
    public DbSet<EmailConfig> EmailConfigs => Set<EmailConfig>();
    public DbSet<SMSConfig> SMSConfigs => Set<SMSConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        JsonSerializerOptions jsonOptions = new();

        base.OnModelCreating(modelBuilder);

        ValueComparer<DeliveryResultDTO?> deliveryResultComparer = new(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => c == null ? null : JsonSerializer.Deserialize<DeliveryResultDTO>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions));

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ChallengeLifetimeMinutes).HasDefaultValue(5);
            entity.Property(e => e.ResendCooldownSeconds).HasDefaultValue(60);
            entity.Property(e => e.MaxVerificationAttempts).HasDefaultValue(5);
            entity.Property(e => e.MaxRequestsPerWindow).HasDefaultValue(5);
            entity.Property(e => e.IssueWindowMinutes).HasDefaultValue(60);

            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Tenants_Name");
        });

        modelBuilder.Entity<TenantAPIKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.KeyPreview).IsRequired().HasMaxLength(18);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64);

            entity.HasIndex(e => new
            {
                e.TenantId,
                e.KeyHash
            })
            .IsUnique()
            .HasDatabaseName("UX_TenantAPIKeys_KeyHash");
            
            entity.HasIndex(e => new
            {
                e.TenantId,
                e.ExpiresAt
            })
            .HasDatabaseName("IX_TenantAPIKeys_TenantId_ExpiresAt");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.Subject).IsRequired(false).HasMaxLength(256);
            entity.Property(e => e.Body).IsRequired();

            entity
                .HasAlternateKey(t => new
                {
                    t.TenantId,
                    t.Channel,
                    t.Name
                })
                .HasName("UX_Templates_TenantId_Channel_Name");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ValueComparer<List<Dictionary<string, object>>> dataListComparer = new(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
            c => JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<List<Dictionary<string, object>>>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions)!);

        ValueComparer<Dictionary<string, object>> dataComparer = new(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
            c => JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions)!);

        modelBuilder.Entity<MessageRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.TemplateName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Channels).IsRequired();
            entity.Property(e => e.Data)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<Dictionary<string, object>>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(dataListComparer);
            entity.OwnsMany(e => e.Recipients, owned =>
            {
                owned.ToJson();
            });
            entity.Property(e => e.DeliveryResult)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<DeliveryResultDTO>(v, jsonOptions))
                .Metadata.SetValueComparer(deliveryResultComparer);

            entity.HasIndex(e => new
            {
                e.TenantId,
                e.CreatedAt
            })
            .HasDatabaseName("IX_MessageRequests_TenantId_At");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Template)
                .WithMany()
                .HasForeignKey(m => new { m.TenantId, m.Channels, m.TemplateName })
                .HasPrincipalKey(t => new { t.TenantId, t.Channel, t.Name });

            entity.HasOne(e => e.SenderEmail)
                .WithMany()
                .HasForeignKey(e => e.SenderEmailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SenderSMS)
                .WithMany()
                .HasForeignKey(e => e.SenderSMSId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OTPChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.TemplateName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Channels).IsRequired();
            entity.Property(e => e.RecipientHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Data)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new())
                .Metadata.SetValueComparer(dataComparer);
            entity.OwnsOne(e => e.Recipient, owned =>
            {
                owned.ToJson();
            });
            entity.Navigation(e => e.Recipient).IsRequired();
            entity.Property(e => e.DeliveryResult)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<DeliveryResultDTO>(v, jsonOptions))
                .Metadata.SetValueComparer(deliveryResultComparer);
            entity.HasIndex(e => new
            {
                e.TenantId,
                e.RecipientHash,
                e.Status,
                e.ExpiresAt
            })
            .HasDatabaseName("IX_OTPChallenges_Tenant_Destination_Status_ExpiresAt");

            entity.HasIndex(e => new
            {
                e.TenantId,
                e.RecipientHash,
                e.CreatedAt
            })
            .HasDatabaseName("IX_OTPChallenges_Tenant_Destination_IssuedAt");

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Template)
                .WithMany()
                .HasForeignKey(m => new { m.TenantId, m.Channels, m.TemplateName })
                .HasPrincipalKey(t => new { t.TenantId, t.Channel, t.Name });

            entity.HasOne(e => e.SenderEmail)
                .WithMany()
                .HasForeignKey(e => e.SenderEmailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SenderSMS)
                .WithMany()
                .HasForeignKey(e => e.SenderSMSId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmailConfig>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Host).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FromAddress).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FromName).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SMSConfig>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FromNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.APIKey).HasMaxLength(256);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
