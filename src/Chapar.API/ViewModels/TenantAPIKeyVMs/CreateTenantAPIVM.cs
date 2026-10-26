using System.ComponentModel.DataAnnotations;

namespace Chapar.API.ViewModels.TenantAPIKeyVMs;

public sealed record CreateTenantAPIVM
{
    [Required]
    public string DisplayName { get; set; } = null!;

    public DateTimeOffset ExpiresAt{ get; set; }
}
