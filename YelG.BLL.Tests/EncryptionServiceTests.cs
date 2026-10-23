using System.Security.Cryptography;
using YelG.DAL.Access.Helpers;

namespace YelG.BLL.Tests
{
    public class EncryptionServiceTests
    {
        private static EncryptionService NewService(string? key = null) =>
            new(new EncryptionOptions { Key = key ?? EncryptionService.GenerateKey() });

        [Theory]
        [InlineData("سر الشركة 123")]
        [InlineData("")]
        [InlineData("a longer value with symbols !@#$%^&*() and new\nlines")]
        public void Round_trips_any_text_including_arabic_and_empty(string text)
        {
            var service = NewService();
            Assert.Equal(text, service.Decrypt(service.Encrypt(text)));
        }

        [Fact]
        public void The_same_text_encrypts_differently_every_time()
        {
            var service = NewService();
            var first = service.Encrypt("نفس النص");
            var second = service.Encrypt("نفس النص");

            Assert.StartsWith("v1.", first);
            Assert.NotEqual(first, second);   // fresh random nonce per call
        }

        [Fact]
        public void A_changed_text_or_another_key_is_rejected()
        {
            var service = NewService();
            var encrypted = service.Encrypt("قيمة محمية");

            var bytes = Convert.FromBase64String(encrypted["v1.".Length..]);
            bytes[^1] ^= 0x01;   // flip one bit of the ciphertext
            var tampered = "v1." + Convert.ToBase64String(bytes);

            Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tampered));
            Assert.ThrowsAny<CryptographicException>(() => NewService().Decrypt(encrypted));
            Assert.ThrowsAny<CryptographicException>(() => service.Decrypt("plain text"));
            Assert.ThrowsAny<CryptographicException>(() => service.Decrypt("v1.AAAA"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("not base64!")]
        [InlineData("c2hvcnQ=")]   // "short": 5 bytes, not 32
        public void A_missing_or_wrong_key_fails_clearly(string? key)
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                new EncryptionService(new EncryptionOptions { Key = key }));
            Assert.Contains("Encryption:Key", error.Message);
        }
    }
}
