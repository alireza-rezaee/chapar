using MailKit.Security;

namespace Chapar.API.Enums;

public enum SmtpSecureSocketOption
{
    /// <summary>
    /// No SSL or TLS encryption should be used.
    /// </summary>
    None = SecureSocketOptions.None,

    /// <summary>
    /// Allow the <see cref="IMailService"/> to decide which SSL or TLS
    /// options to use (default). If the server does not support SSL or TLS,
    /// then the connection will continue without any encryption.
    /// </summary>
    Auto = SecureSocketOptions.Auto,

    /// <summary>
    /// The connection should use SSL or TLS encryption immediately.
    /// </summary>
    SslOnConnect = SecureSocketOptions.SslOnConnect,

    /// <summary>
    /// Elevates the connection to use TLS encryption immediately after
    /// reading the greeting and capabilities of the server. If the server
    /// does not support the STARTTLS extension, then the connection will
    /// fail and a <see cref="System.NotSupportedException"/> will be thrown.
    /// </summary>
    StartTls = SecureSocketOptions.StartTls,

    /// <summary>
    /// Elevates the connection to use TLS encryption immediately after
    /// reading the greeting and capabilities of the server, but only if
    /// the server supports the STARTTLS extension.
    /// </summary>
    StartTlsWhenAvailable = SecureSocketOptions.StartTlsWhenAvailable,
}
