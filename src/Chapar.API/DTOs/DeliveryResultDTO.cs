namespace Chapar.API.DTOs;

public record DeliveryResultDTO
{
    public bool IsSucceeded { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    public static DeliveryResultDTO Success(object? data = null) => new()
    {
        IsSucceeded = true,
        Data = data,
    };

    public static DeliveryResultDTO Failure(string errorMessage, object? data = null) => new()
    {
        IsSucceeded = false,
        ErrorMessage = errorMessage,
        Data = data,
    };

    public static DeliveryResultDTO Merge(IEnumerable<DeliveryResultDTO> inner) => new()
    {
        IsSucceeded = inner.All(x => x.IsSucceeded),
        ErrorMessage = inner.Any(x => !x.IsSucceeded)
            ? "One or more errors occurred."
            : null,
        Data = inner,
        Time = inner.Max(x => x.Time),
    };
};
