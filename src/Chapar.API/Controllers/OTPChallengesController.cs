using System.Security.Cryptography;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Chapar.API.Attributes;
using Chapar.API.Enums;
using Chapar.API.Helpers;
using Chapar.API.Models;
using Chapar.API.Options;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.OTPChallengeVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/otp-challenges")]
public class OTPChallengesController
(
    IPublishEndpoint publishEndpoint,
    IOptions<OTPOptions> otpOptions,
    ITenantBasedRepository<OTPChallenge, Guid> repository,
    IUnitOfWork unitOfWork,
    ILogger<OTPChallengesController> logger
) : ControllerBase
{
    const string GetRoute = nameof(OTPChallengesController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<OTPChallenge>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedItemsVM<OTPChallenge>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] Guid tenantId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.ListAsync(tenantId, pagination, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{id:Guid}", Name = GetRoute)]
    [ProducesResponseType(typeof(OTPChallenge), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.GetByIdAsync(id, tenantId, cancellationToken: cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CreateOTPResponseVM), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid tenantId,
        [FromBody] CreateOTPRequestVM dto,
        CancellationToken cancellationToken = default)
    {
        byte[] recipientHash = OTPHelper.HashRecipient(
            otpOptions.Value.HashingKey, dto.Recipient);

        Channel[] requestedChannels = [.. Enum.GetValues<Channel>().Where(
            channel => channel != 0 && dto.Channels.HasFlag(channel))];

        if (requestedChannels.Length != 1)
        {
            logger.LogWarning("One channel must be selected.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Channel Selection",
                Detail = "One channel must be selected."
            });
        }

        OTPChallenge entity = new()
        {
            TenantId = tenantId,
            TemplateName = dto.TemplateName,
            Channels = dto.Channels,
            Recipient = dto.Recipient,
            RecipientHash = recipientHash,
            CodeHash = [],
            Status = OTPChallengeStatus.None,
            Attempts = 0,
            ExpiresAt = DateTimeOffset.Now.AddSeconds(dto.LifeTimeSeconds)
        };

        await repository.AddAsync(entity, tenantId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(entity, cancellationToken);

        return CreatedAtRoute(
            GetRoute,
            new { tenantId, id = entity.Id },
            entity
        );
    }

    [HttpPost("{id:Guid}/verify")]
    [ProducesResponseType(typeof(VerificationOTPResponseVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyCodeAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] VerificationOTPRequestVM request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await repository.GetByIdAsync(
                id: id,
                tenantId: tenantId,
                useCache: false,
                cancellationToken: cancellationToken);

            if (entity.Status == OTPChallengeStatus.Verified)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "OTP already verified",
                    Detail = "This OTP request has already been used and verified."
                });
            }

            if (entity.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "OTP expired",
                    Detail = "The OTP code has expired. Please request a new one."
                });
            }

            if (entity.Attempts >= otpOptions.Value.MaxAttempts)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Too many failed attempts",
                    Detail = "Maximum verification attempts exceeded. Please generate a new OTP."
                });
            }

            byte[] possibleCodeHash = OTPHelper.HashOTPCode(
                otpOptions.Value.HashingKey, tenantId, entity.Id, request.Code);

            bool isCodeValid = CryptographicOperations.FixedTimeEquals(
                possibleCodeHash, entity.CodeHash);

            if (!isCodeValid)
            {
                await repository.ExecuteUpdateAsync
                (
                    id: id,
                    tenantId: tenantId,
                    updatedAt: entity.UpdatedAt,
                    setter => setter
                        .SetProperty(x => x.Attempts, entity.Attempts + 1),
                    cancellationToken: cancellationToken
                );

                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid code",
                    Detail = "The verification code is incorrect."
                });
            }

            DateTimeOffset utcNow = DateTimeOffset.Now;
            await repository.ExecuteUpdateAsync
            (
                id: id,
                tenantId: tenantId,
                updatedAt: entity.UpdatedAt,
                setter => setter
                    .SetProperty(x => x.Status, OTPChallengeStatus.Verified)
                    .SetProperty(x => x.VerifiedAt, utcNow),
                cancellationToken: cancellationToken
            );

            VerificationOTPResponseVM response = new() { IsVerified = true };
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }
}
