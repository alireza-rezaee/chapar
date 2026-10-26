namespace Chapar.API.Models.IHas;

public interface IHasTenant
{
    Tenant Tenant { get; set; }
    Guid TenantId { get; set; }
}
