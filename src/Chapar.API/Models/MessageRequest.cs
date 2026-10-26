using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class MessageRequest
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public Template Template { get; set; } = null!;
    public Channel Channels { get; set; }
    public List<Dictionary<string, object>> Data { get; init; } = [];
    public List<RecipientInfoDTO> Recipients { get; set; } = [];
    public DateTimeOffset? ToSendAtAfter { get; set; } // TODO:
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DeliveryResultDTO? DeliveryResult { get; set; }
    public Guid? SenderEmailId { get; set; }
    public EmailConfig? SenderEmail { get; set; }
    public Guid? SenderSMSId { get; set; }
    public SMSConfig? SenderSMS { get; set; }
}
