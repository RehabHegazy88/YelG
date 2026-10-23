using System.Security.Cryptography;
using System.Text;

namespace YelG.DAL.Access.Helpers
{
    /// <summary>Bound from the "Encryption" configuration section (user-secrets in development, environment variables on servers).</summary>
    public sealed class EncryptionOptions
    {
        /// <summary>32 random bytes in Base64 (AES-256). Generate one with <see cref="EncryptionService.GenerateKey"/>.</summary>
        public string? Key { get; init; }
    }

    /// <summary>Reversible encryption of short secrets stored by the application (not for passwords: use Identity or BCrypt).</summary>
    public interface IEncryptionService
    {
        string Encrypt(string plainText);

        /// <exception cref="CryptographicException">The text was changed, cut, or encrypted with another key.</exception>
        string Decrypt(string cipherText);
    }

    /// <summary>
    /// AES-256-GCM: authenticated encryption, so any change to the stored text is detected on decrypt instead of
    /// producing garbage. Every call uses a fresh random 12-byte nonce, so the same input never encrypts the same
    /// way twice. Output: "v1." + Base64(nonce | tag | ciphertext). The "v1." prefix leaves room for a future
    /// format or key rotation without guessing.
    /// </summary>
    public sealed class EncryptionService : IEncryptionService
    {
        private const string Version = "v1.";
        private const int KeySize = 32;
        private const int NonceSize = 12;   // AesGcm.NonceByteSizes.MaxSize
        private const int TagSize = 16;     // AesGcm.TagByteSizes.MaxSize

        private readonly byte[] _key;

        public EncryptionService(EncryptionOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Key))
                throw new InvalidOperationException(
                    "Encryption:Key is not configured. Set it in user-secrets (development) or the Encryption__Key " +
                    "environment variable (servers) to 32 random bytes in Base64.");
            try
            {
                _key = Convert.FromBase64String(options.Key);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Encryption:Key must be Base64.");
            }
            if (_key.Length != KeySize)
                throw new InvalidOperationException($"Encryption:Key must be {KeySize} bytes (AES-256); it is {_key.Length}.");
        }

        /// <summary>A new random key, Base64, for the configuration.</summary>
        public static string GenerateKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize));

        public string Encrypt(string plainText)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            var plain = Encoding.UTF8.GetBytes(plainText);
            var output = new byte[NonceSize + TagSize + plain.Length];
            var nonce = output.AsSpan(0, NonceSize);
            var tag = output.AsSpan(NonceSize, TagSize);
            var cipher = output.AsSpan(NonceSize + TagSize);

            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plain, cipher, tag);
            return Version + Convert.ToBase64String(output);
        }

        public string Decrypt(string cipherText)
        {
            ArgumentNullException.ThrowIfNull(cipherText);
            if (!cipherText.StartsWith(Version, StringComparison.Ordinal))
                throw new CryptographicException("Unknown encrypted text format.");

            byte[] input;
            try
            {
                input = Convert.FromBase64String(cipherText[Version.Length..]);
            }
            catch (FormatException)
            {
                throw new CryptographicException("The encrypted text is not valid Base64.");
            }
            if (input.Length < NonceSize + TagSize)
                throw new CryptographicException("The encrypted text is too short.");

            var plain = new byte[input.Length - NonceSize - TagSize];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(input.AsSpan(0, NonceSize), input.AsSpan(NonceSize + TagSize), input.AsSpan(NonceSize, TagSize), plain);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
