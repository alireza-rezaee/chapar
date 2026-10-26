using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class OTPChallenge
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasTenant
    , IHasExpiresAt
    , IHasVerifiedAt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public Template Template { get; set; } = null!;
    public required Channel Channels { get; set; }
    public Dictionary<string, object> Data { get; init; } = [];
    public required RecipientInfoDTO Recipient { get; set; }
    public Guid? SenderEmailId { get; set; }
    public EmailConfig? SenderEmail { get; set; }
    public Guid? SenderSMSId { get; set; }
    public SMSConfig? SenderSMS { get; set; }
    public required byte[] RecipientHash { get; set; }
    public required byte[] CodeHash { get; set; }
    public OTPChallengeStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DeliveryResultDTO? DeliveryResult { get; set; }
}
