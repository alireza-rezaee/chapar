using Chapar.API.DTOs;
using Chapar.API.Options;

namespace Chapar.API.Services;

public interface IEmailService
{
    public Task<DeliveryResultDTO> SendAsync(
        MailAddressDTO[] recipients,
        string subject,
        string body,
        SmtpSenderOptions options,
        bool isHtmlBody = false,
        IAsyncEnumerable<FileDTO>? attachments = null,
        CancellationToken cancellationToken = default);
}
