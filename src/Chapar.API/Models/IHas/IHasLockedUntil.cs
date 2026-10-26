namespace Chapar.API.Models.IHas;

public interface IHasLockedUntil
{
    public DateTimeOffset? LockedUntil { get; set; }
}
