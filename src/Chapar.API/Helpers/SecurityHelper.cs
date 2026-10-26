using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Chapar.API.Helpers;

public static class SecurityHelper
{
    private static readonly byte Separator = 0x1F;

    public static byte[] ComputeHmacSha256(byte[] masterKey, params object[] parts)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        ArgumentNullException.ThrowIfNull(parts);

        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, masterKey);

        Span<byte> scratchBuffer = stackalloc byte[16];
        ReadOnlySpan<byte> separatorSpan = [Separator];

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part is null) continue;

            if (i > 0)
                hmac.AppendData(separatorSpan);

            switch (part)
            {
                case Guid guid:
                    guid.TryWriteBytes(scratchBuffer);
                    hmac.AppendData(scratchBuffer);
                    break;

                case string str:
                    hmac.AppendData(Encoding.UTF8.GetBytes(str));
                    break;

                case byte[] bytes:
                    hmac.AppendData(bytes);
                    break;

                case ReadOnlyMemory<byte> mem:
                    hmac.AppendData(mem.Span);
                    break;

                case int num:
                    BinaryPrimitives.WriteInt32LittleEndian(scratchBuffer[..4], num);
                    hmac.AppendData(scratchBuffer[..4]);
                    break;

                case long num:
                    BinaryPrimitives.WriteInt64LittleEndian(scratchBuffer[..8], num);
                    hmac.AppendData(scratchBuffer[..8]);
                    break;

                case DateTimeOffset dto:
                    BinaryPrimitives.WriteInt64LittleEndian(scratchBuffer[..8], dto.ToUnixTimeSeconds());
                    hmac.AppendData(scratchBuffer[..8]);
                    break;

                default:
                    hmac.AppendData(Encoding.UTF8.GetBytes(part.ToString() ?? string.Empty));
                    break;
            }
        }

        return hmac.GetHashAndReset();
    }

    public static string ComputeHmacSha256Hex(byte[] masterKey, params object[] parts)
    {
        byte[] hash = ComputeHmacSha256(masterKey, parts);
        return Convert.ToHexString(hash);
    }
}
