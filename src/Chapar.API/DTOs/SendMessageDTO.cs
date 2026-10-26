using Chapar.API.Models;

namespace Chapar.API.DTOs;

public record SendMessageDTO
{
    public required string Message { get; init; }
    public required string Destination { get; init; }
    public string? Subject { get; init; }
    public string? FullName { get; set; }
}
