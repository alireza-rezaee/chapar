using System.Text.Json.Serialization;
using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.ViewModels.EmailConfigVMs;

public record CreateEditEmailConfigVM
    : IHasUpdatedAt
{
    public required string Name { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public required string FromName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SmtpSecureSocketOption SocketOption { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
