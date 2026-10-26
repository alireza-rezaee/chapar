using System.Security.Cryptography;
using System.Text;
using Chapar.API.DTOs;

namespace Chapar.API.Helpers;

public static class OTPHelper
{
    public static byte[] HashOTPCode(
        string masterKey,
        Guid tenantId,
        Guid otpId,
        string otpCode)
    {
        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);

        byte[] computedHash = SecurityHelper.ComputeHmacSha256(
            masterKeyBytes, 
            tenantId, 
            otpId, 
            otpCode
        );

        return computedHash;
    }

    public static byte[] HashRecipient(
        string masterKey,
        RecipientInfoDTO recipient)
    {
        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);

        byte[] computedHash = SecurityHelper.ComputeHmacSha256(
            masterKeyBytes, 
            recipient
        );

        return computedHash;
    }

    public static string GererateOTPCode(int length = 6)
    {
        int min = (int)Math.Pow(10, length - 1);
        int max = (int)Math.Pow(10, length);

        int otp = RandomNumberGenerator.GetInt32(min, max);
        return otp.ToString();
    }
}
