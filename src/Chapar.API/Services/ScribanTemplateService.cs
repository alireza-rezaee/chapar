using Microsoft.EntityFrameworkCore;
using Chapar.API.Data;
using Chapar.API.DTOs;
using Chapar.API.Enums;
using Scriban;

namespace Chapar.API.Services;

public class ScribanTemplateService
(
    ApplicationDbContext dbContext
    // ILogger<ScribanTemplateService> logger // TODO
) : ITemplateService
{
    public async Task<RenderedTemplateDTO> RenderAsync(
        Guid tenantId,
        string templateName,
        Channel channel,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        var (subjectTemplate, messageTemplate) = await GetTemplateAsync(
            tenantId, templateName, channel, cancellationToken);

        string message = await messageTemplate.RenderAsync(data);
        
        string? subject = subjectTemplate is not null 
            ? await subjectTemplate.RenderAsync(data) 
            : null;

        return new RenderedTemplateDTO
        (
            Message: message,
            Subject: subject
        );
    }

    public async Task<IEnumerable<RenderedTemplateDTO>> RenderAsync(
        Guid tenantId,
        string templateName,
        Channel channel,
        IEnumerable<Dictionary<string, object>> data,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<RenderedTemplateDTO>> tasks = data.Select(item =>
            RenderAsync(tenantId, templateName, channel, item, cancellationToken));

        RenderedTemplateDTO[] results = await Task.WhenAll(tasks);

        return results;
    }

    private async Task<(Template? Subject, Template Message)> GetTemplateAsync(
        Guid tenantId,
        string templateName,
        Channel channel,
        CancellationToken cancellationToken = default)
    {
        var enity = await dbContext.Templates.FirstOrDefaultAsync(x =>
            x.Name == templateName && x.Channel == channel && x.TenantId == tenantId,
            cancellationToken) ?? throw new KeyNotFoundException(); // TODO:

        Template? subject = string.IsNullOrWhiteSpace(enity.Subject)
            ? null
            : Template.Parse(enity.Subject);

        Template message = Template.Parse(enity.Body);

        return (subject, message);
    }
}
