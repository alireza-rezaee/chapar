namespace Chapar.API.Models.IHas;

public interface IHasId<TId>
{
    TId Id { get; set; }
}
