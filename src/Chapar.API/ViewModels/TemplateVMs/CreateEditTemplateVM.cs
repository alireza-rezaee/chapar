using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.ViewModels.TemplateVMs;

public sealed record CreateEditTemplateVM
    : IHasUpdatedAt
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Channel Channel { get; set; }

    [Required, MinLength(1)]
    public string? Subject { get; set; }

    [Required]
    public string Body { get; set; } = null!;

    public DateTimeOffset UpdatedAt { get; set; }
}
