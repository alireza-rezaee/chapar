using MailKit.Net.Smtp;
using MimeKit;
using Chapar.API.DTOs;
using Chapar.API.Options;
using Chapar.API.Enums;
using System.Collections.Concurrent;
using Chapar.API.Helpers;
using MailKit.Security;

namespace Chapar.API.Services;

public class SmtpEmailService(
    ILogger<SmtpEmailService> logger
) : IEmailService
  , IMessageChannelService
  , IOTPChannelService
{
    public Channel SupportedChannels => Channel.Email;

    public async Task<DeliveryResultDTO> SendMessageByAsync(
        Channel channel,
        SendMessageDTO message,
        SenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config.Email);

        if (channel.HasFlag(Channel.Email))
            return await SendMessageByEmailAsync(message, config.Email, cancellationToken);

        else
            throw new NotSupportedException();
    }

    public async Task<DeliveryResultDTO> SendMessagesByAsync(
        Channel channel,
        IEnumerable<SendMessageDTO> messages,
        SenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config.Email);

        if (channel.HasFlag(Channel.Email))
            return await SendMessagesByEmailAsync(messages, config.Email, cancellationToken);

        else
            throw new NotSupportedException();
    }

    public async Task<DeliveryResultDTO> SendOTPByAsync(
        Channel channel,
        SendMessageDTO request,
        SenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentNullException.ThrowIfNull(config.Email);

        string code = OTPHelper.GererateOTPCode();
        SendMessageDTO dto = new()
        {
            Destination = request.Destination,
            FullName = request.FullName,
            Message = request.Message.Replace("{{code}}", code, StringComparison.OrdinalIgnoreCase),
            Subject = request.Message.Replace("{{code}}", code, StringComparison.OrdinalIgnoreCase),
        };

        if (channel.HasFlag(Channel.Email))
            return await SendMessageByEmailAsync(dto, config.Email, cancellationToken);

        else
            throw new NotSupportedException();
    }

    public async Task<DeliveryResultDTO> SendMessageByEmailAsync(
        SendMessageDTO message,
        SmtpSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);

        MailAddressDTO destination = new
        (
            Name: message.FullName ?? message.Destination,
            Address: message.Destination
        );

        return await SendAsync(
            recipients: [destination],
            subject: message.Subject,
            body: message.Message,
            config: config,
            isHtmlBody: true,
            attachments: null,
            cancellationToken: cancellationToken);
    }

    public async Task<DeliveryResultDTO> SendMessagesByEmailAsync(
        IEnumerable<SendMessageDTO> messages,
        SmtpSenderOptions options,
        CancellationToken cancellationToken = default)
    {
        ConcurrentBag<DeliveryResultDTO> results = [];
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 2 };
        await Parallel.ForEachAsync(messages, parallelOptions, async (message, token) =>
        {
            DeliveryResultDTO result = await SendMessageByEmailAsync(message, options, cancellationToken);
            results.Add(result);
        });

        logger.LogDebug("Email sent to recipient(s) done.");
        return DeliveryResultDTO.Success(new { Results = results.ToArray() });
    }

    public async Task<DeliveryResultDTO> SendAsync(
        MailAddressDTO[] recipients,
        string subject,
        string body,
        SmtpSenderOptions config,
        bool isHtmlBody = false,
        IAsyncEnumerable<FileDTO>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using MimeMessage message = new();

            MailboxAddress from = new(config.FromName, config.FromAddress);
            message.From.Add(from);

            IEnumerable<MailboxAddress> to = recipients.Select(x
                => new MailboxAddress(x.Name, x.Address));
            message.To.AddRange(to);

            message.Subject = subject;

            BodyBuilder bodyBuilder = new();

            if (isHtmlBody)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;

            if (attachments is not null
                && await attachments.AnyAsync(cancellationToken))
            {
                await foreach (var (name, stream) in attachments)
                    await bodyBuilder.Attachments.AddAsync(name, stream, cancellationToken);
            }

            message.Body = bodyBuilder.ToMessageBody();

            using SmtpClient client = new();
            await client.ConnectAsync(
                host: config.Host,
                port: config.Port,
                options: (SecureSocketOptions)config.SocketOption,
                cancellationToken: cancellationToken);

            await client.AuthenticateAsync(
                userName: config.Username,
                password: config.Password,
                cancellationToken: cancellationToken);

            await client.SendAsync(message);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogDebug("Email sent to recipient(s).");
            return DeliveryResultDTO.Success(new { message.MessageId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send emails to recipient(s).");
            return DeliveryResultDTO.Failure(errorMessage: ex.Message);
        }
    }
}

public record EmailDestinationText(MailAddressDTO Destination, string Subject, string Text);
