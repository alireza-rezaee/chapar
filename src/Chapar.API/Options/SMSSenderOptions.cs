namespace Chapar.API.Options;

public sealed class SMSSenderOptions
{
    public required string FromNumber { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? APIKey { get; set; }
}
