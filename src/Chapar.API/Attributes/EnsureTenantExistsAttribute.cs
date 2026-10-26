using Microsoft.AspNetCore.Mvc;
using Chapar.API.ActionFilters;

namespace Chapar.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class EnsureTenantAccessibilityAttribute : TypeFilterAttribute
{
    public EnsureTenantAccessibilityAttribute()
        : base(typeof(EnsureTenantAccessibilityFilter))
    {
    }
}
