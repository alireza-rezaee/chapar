namespace Chapar.API.Options;

public sealed class OTPOptions
{
    public required string HashingKey { get; init; }
    public required int MaxAttempts { get; set; } = 5;
}
