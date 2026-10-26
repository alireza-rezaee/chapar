using System.ComponentModel.DataAnnotations;

namespace Chapar.API.ViewModels;

public record PaginationParams
{
    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value;
    }
}
