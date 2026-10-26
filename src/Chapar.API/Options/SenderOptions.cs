namespace Chapar.API.Options;

public sealed class SenderOptions
{
    public SMSSenderOptions? SMS { get; set; }
    public SmtpSenderOptions? Email { get; set; }
}
