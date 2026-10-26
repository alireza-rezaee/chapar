namespace Chapar.API.Enums;

[Flags]
public enum Channel
{
    SMS = 1 << 0,
    Email = 1 << 1,
    VoiceCall = 1 << 2,
}
