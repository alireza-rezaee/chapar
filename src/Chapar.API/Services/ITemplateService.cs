using Chapar.API.DTOs;
using Chapar.API.Enums;

namespace Chapar.API.Services;

public interface ITemplateService
{
    Task<RenderedTemplateDTO> RenderAsync(
        Guid tenantId,
        string templateName,
        Channel channel,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<RenderedTemplateDTO>> RenderAsync(
        Guid tenantId,
        string templateName,
        Channel channel,
        IEnumerable<Dictionary<string, object>> data,
        CancellationToken cancellationToken = default);
}
