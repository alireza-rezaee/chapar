namespace Chapar.API.ViewModels;

public record PagedItemsVM<T>(int TotalCount, IEnumerable<T> Items);
