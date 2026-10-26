using Chapar.API.Models.IHas;

namespace Chapar.API.ViewModels.SMSConfigVMs;

public record CreateEditSMSConfigVM
    : IHasUpdatedAt
{
    public required string Name { get; set; }
    public required string FromNumber { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? APIKey { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
