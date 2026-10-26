using Chapar.API.Enums;

namespace Chapar.API.Options;

public sealed class SmtpSenderOptions
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public required string FromName { get; set; }
    public SmtpSecureSocketOption SocketOption { get; set; }
}
