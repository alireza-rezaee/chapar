using Microsoft.EntityFrameworkCore;
using Chapar.API.Data;
using Chapar.API.Models;
using Chapar.API.Options;

namespace Chapar.API.Services.Repositories;

public class OTPRepository
(
    ApplicationDbContext dbContext
    // ILogger<BasicRepository<EntityT, IdT>> logger
) : TenantBasedRepository<OTPChallenge, Guid>(dbContext)
{
    public async Task<SenderOptions> GetSenderOptionsForMessageAsync(Guid id, Guid tenantId)
    {
        var configs = await EntitySet
            .AsNoTracking()
            .Where(x => x.Id.Equals(id) && x.TenantId.Equals(tenantId))
            .Select(x => new 
            {
                SMS = x.SenderSMS, 
                Email = x.SenderEmail
            })
            .FirstOrDefaultAsync()
            ?? throw IdNotFoundException(id);

        return new SenderOptions()
        {
            Email = configs.Email is EmailConfig emailConfig
                ? new SmtpSenderOptions()
                {
                    Host = emailConfig.Host,
                    Port = emailConfig.Port,
                    SocketOption = emailConfig.SocketOption,
                    Username = emailConfig.Username,
                    Password = emailConfig.Password,
                    FromAddress = emailConfig.FromAddress,
                    FromName = emailConfig.Name,
                }
                : null,
            SMS = configs.SMS is SMSConfig smsConfig
                ? new SMSSenderOptions()
                {
                    Username = smsConfig.Username,
                    Password = smsConfig.Password,
                    APIKey = smsConfig.APIKey,
                    FromNumber = smsConfig.FromNumber,
                }
                : null,
        };
    }
}
