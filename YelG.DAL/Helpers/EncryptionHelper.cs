using System;
using System.Web;

namespace YelG.DAL.Access.Helpers
{
    using BCrypt =  BCrypt.Net.BCrypt;

    public static class EncryptionHelper
    {
        public static String UrlEncode(this String str)
        {
            return HttpUtility.UrlEncode(str);
        }
        public static String UrlDecode(this String str)
        {
            return Uri.UnescapeDataString(str);
        }
        // Encrypt / Decrypt (AES-CBC with a hard-coded key and fixed salt from another project) were replaced
        // by IEncryptionService: AES-256-GCM with the key from configuration (Encryption:Key).

        public static string WithBCrypt(this string text)
        {
            var result = BCrypt.HashPassword(text);
            return result;
        }

        public static bool VerifyWithBCrypt(this string hashedPassword, string plainText)
        {
            var result = BCrypt.Verify(plainText, hashedPassword);
            return result;
        }
    }
}