using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Helpers;
using Chapar.API.Options;

namespace Chapar.API.Services;

public class MelipayamakService(
    ILogger<MelipayamakService> logger
) : ISMSService
  , IMessageChannelService
  , IOTPChannelService
{
    public Channel SupportedChannels => Channel.SMS | Channel.VoiceCall;

    private readonly Uri ConsoleAPIUrl = new("https://console.melipayamak.com");
    private readonly Uri APIUrl = new("http://api.payamak-panel.com");

    private readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<DeliveryResultDTO> SendMessageByAsync(
        Channel channel,
        SendMessageDTO message,
        SenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config.SMS);

        if (channel.HasFlag(Channel.SMS))
            return await SendMessageBySMSAsync(
                message.Message,
                message.Destination,
                config.SMS,
                cancellationToken);

        else if (channel.HasFlag(Channel.VoiceCall))
            return await SendMessageByVoiceCallAsync(
                message.Message,
                message.Destination,
                config.SMS,
                cancellationToken);

        else
            throw new NotSupportedException();
    }

    public async Task<DeliveryResultDTO> SendMessagesByAsync(
        Channel channel,
        IEnumerable<SendMessageDTO> messages,
        SenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config.SMS);

        if (channel.HasFlag(Channel.SMS))
            return await SendMessagesBySMSAsync(messages, config.SMS, cancellationToken);

        else if (channel.HasFlag(Channel.VoiceCall))
            return await SendMessagesByVoiceCallAsync(messages, config.SMS, cancellationToken);

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
        ArgumentNullException.ThrowIfNull(config.SMS);

        if (channel.HasFlag(Channel.SMS))
            return await SendOTPBySMSAsync(request.Destination, config.SMS, cancellationToken);

        else if (channel.HasFlag(Channel.VoiceCall))
            return await SendOTPByCallAsync(request, config.SMS, cancellationToken);
            
        else
            throw new NotSupportedException();
    }

    public async Task<DeliveryResultDTO> SendMessageBySMSAsync(
        string message,
        string destination,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync([destination], message, config, cancellationToken);
    }

    public async Task<DeliveryResultDTO> SendMessageByVoiceCallAsync(
        string message,
        string destination,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        return await CallAsync(destination, message, config, cancellationToken);
    }

    public async Task<DeliveryResultDTO> SendMessagesBySMSAsync(
        IEnumerable<SendMessageDTO> messages,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        DestinationText[] recipientTexts = [..
            messages.Select(x => new DestinationText
            (
                Destination: x.Destination,
                Text: x.Message)
            )];

        return await SendMultipleAsync(recipientTexts, config, cancellationToken);
    }

    public async Task<DeliveryResultDTO> SendMessagesByVoiceCallAsync(
        IEnumerable<SendMessageDTO> messages,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        return await CallsAsync(messages, config, cancellationToken);
    }

    public async Task<DeliveryResultDTO> SendOTPBySMSAsync(
        string toNumber,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toNumber);

        try
        {
            using HttpClient client = new() { BaseAddress = ConsoleAPIUrl };
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                requestUri: $"api/send/otp/{config.APIKey}",
                value: new TextOTPRequest(To: toNumber),
                options: SerializerOptions,
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TextOTPResponse>(
                options: SerializerOptions,
                cancellationToken: cancellationToken) ?? throw new FormatException(
                    "Response format is not supported.");

            if (string.IsNullOrWhiteSpace(result.Code))
                throw new InvalidOperationException(
                    result.Status ?? "OTP provider returned no code.");

            logger.LogDebug("OTP sent to recipient(s). Result: {Result}", result);
            return DeliveryResultDTO.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP (SMS).");
            return DeliveryResultDTO.Failure(ex.Message);
        }
    }

    public async Task<DeliveryResultDTO> SendOTPByCallAsync(
        SendMessageDTO message,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Destination);

        string code = OTPHelper.GererateOTPCode();
        SendMessageDTO dto = new()
        {
            Destination = message.Destination,
            FullName = message.FullName,
            Message = message.Message.Replace("{{code}}", code, StringComparison.OrdinalIgnoreCase),
            Subject = message.Message.Replace("{{code}}", code, StringComparison.OrdinalIgnoreCase),
        };

        try
        {
            return await SendMessageByVoiceCallAsync(
                message.Message,
                message.Destination,
                config,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP (voice call).");
            return DeliveryResultDTO.Failure(ex.Message);
        }
    }

    public async Task<DeliveryResultDTO> SendAsync(
        string[] toNumbers,
        string text,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {        
        try
        {
            using HttpClient client = new() { BaseAddress = ConsoleAPIUrl };
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                requestUri: $"api/send/simple/{config.APIKey}",
                value: new TextRequest(
                    From: config.FromNumber,
                    To: toNumbers,
                    Text: text),
                options: SerializerOptions,
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TextResponse>(
                options: SerializerOptions,
                cancellationToken: cancellationToken) ?? throw new FormatException(
                    "Response format is not supported.");

            logger.LogDebug("Sent to recipient(s). Result: {Result}", result);
            return DeliveryResultDTO.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to recipient(s).");
            return DeliveryResultDTO.Failure(ex.Message);
        }
    }

    public async Task<DeliveryResultDTO> SendMultipleAsync(
        IEnumerable<DestinationText> recipients,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = new() { BaseAddress = ConsoleAPIUrl };
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                requestUri: $"api/send/multiple/{config.APIKey}",
                value: new MultipleTextsRequest(
                    From: config.FromNumber,
                    To: [.. recipients.Select(x => x.Destination)],
                    Text: [.. recipients.Select(x => x.Text)]),
                options: SerializerOptions,
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MultipleTextsResponse>(
                options: SerializerOptions,
                cancellationToken: cancellationToken) ?? throw new FormatException(
                    "Response format is not supported.");

            logger.LogDebug("Sent to recipient(s). Result: {Result}", result);
            return DeliveryResultDTO.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to recipient(s).");
            return DeliveryResultDTO.Failure(ex.Message);
        }
    }

    public async Task<DeliveryResultDTO> CallsAsync(
        IEnumerable<SendMessageDTO> messages,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {
        ConcurrentBag<DeliveryResultDTO> results = [];
        ParallelOptions options = new() { MaxDegreeOfParallelism = 1 };
        await Parallel.ForEachAsync(messages, options, async (message, token) =>
        {
            DeliveryResultDTO result = await CallAsync(
                message.Destination,
                message.Message,
                config,
                cancellationToken);
            results.Add(result);
        });

        logger.LogDebug("Call to recipient(s) done.");
        return DeliveryResultDTO.Success(new { Results = results.ToArray() });
    }

    public async Task<DeliveryResultDTO> CallAsync(
        string toNumber,
        string text,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default)
    {       
        try
        {
            Dictionary<string, string> parameters = new()
            {
                ["username"] = config.Username!,
                ["password"] = config.APIKey!,
                ["text"] = text,
                ["to"] = toNumber,
            };

            using HttpClient client = new() { BaseAddress = APIUrl };
            using HttpResponseMessage response = await client.PostAsync(
                requestUri: $"/post/Voice.asmx/SendSpeechText",
                content: new FormUrlEncodedContent(parameters),
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TextResponse>(
                options: SerializerOptions,
                cancellationToken: cancellationToken) ?? throw new FormatException(
                    "Response format is not supported.");

            logger.LogDebug("Call to recipient. Result: {Result}", result);
            return DeliveryResultDTO.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call to recipient(s).");
            return DeliveryResultDTO.Failure(ex.Message);
        }
    }

    private record TextRequest(string From, string[] To, string Text);

    private record MultipleTextsRequest(string From, IEnumerable<string> To, IEnumerable<string> Text);

    private record TextResponse()
    {
        [JsonPropertyName(name: "recId")]
        public long[] ReceivedIds { get; init; } = [];

        public string Status { get; init; } = null!;
    }

    private record MultipleTextsResponse()
    {
        [JsonPropertyName(name: "recId")]
        public long[] ReceivedIds { get; init; } = [];

        [JsonPropertyName(name: "success")]
        public bool[] IsSucceeded { get; init; } = [];

        public string Status { get; init; } = null!;
    }

    private record TextOTPRequest(string To);

    private record TextOTPResponse()
    {
        public string? Code { get; init; }

        public string Status { get; init; } = null!;
    }
}
