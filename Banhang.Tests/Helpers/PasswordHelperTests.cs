using Banhang.Helpers;
using Xunit;

namespace Banhang.Tests.Helpers
{
    public class PasswordHelperTests
    {
        [Fact]
        public void HashPassword_ShouldReturnNonEmptyString()
        {
            var password = "TestPassword123";

            var hash = PasswordHelper.HashPassword(password);

            Assert.False(string.IsNullOrEmpty(hash));
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
        {
            var password = "TestPassword123";
            var hash = PasswordHelper.HashPassword(password);

            var result = PasswordHelper.VerifyPassword(password, hash);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ShouldReturnFalse()
        {
            var password = "TestPassword123";
            var wrongPassword = "WrongPassword";
            var hash = PasswordHelper.HashPassword(password);

            var result = PasswordHelper.VerifyPassword(wrongPassword, hash);

            Assert.False(result);
        }
    }
}
