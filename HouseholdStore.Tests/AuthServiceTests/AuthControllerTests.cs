using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Toolify.AuthService.Controllers;
using Toolify.AuthService.Data;
using Toolify.AuthService.DTO;
using Toolify.AuthService.Models;
using Toolify.AuthService.Services;
using Xunit;

namespace HouseholdStore.Tests.AuthServiceTests
{
    public class AuthControllerTests
    {
        private AuthController CreateController(
            Mock<IUserRepository> repoMock,
            Mock<IEmailService> emailMock)
        {
            var config = new Mock<IConfiguration>();

            config.Setup(c => c["Jwt:Key"]).Returns("super_secret_test_key_123456789");
            config.Setup(c => c["Jwt:Issuer"]).Returns("test");
            config.Setup(c => c["Jwt:Audience"]).Returns("test");
            config.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

            return new AuthController(config.Object, repoMock.Object, emailMock.Object);
        }

        // ===== REGISTER =====

        [Fact]
        public void Register_PasswordsMismatch_ReturnsBadRequest()
        {
            var repo = new Mock<IUserRepository>();
            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.Register(new RegisterRequest
            {
                Password = "1",
                ConfirmPassword = "2"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Register_UserExists_ReturnsBadRequest()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.UserExists(It.IsAny<string>())).Returns(true);

            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.Register(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123",
                ConfirmPassword = "123"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Register_Valid_ReturnsOk()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.UserExists(It.IsAny<string>())).Returns(false);

            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.Register(new RegisterRequest
            {
                FirstName = "Ivan",
                LastName = "Ivanov",
                Email = "test@mail.com",
                Phone = "123",
                Password = "123",
                ConfirmPassword = "123"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // ===== LOGIN =====

        [Fact]
        public void Login_Invalid_ReturnsUnauthorized()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((User)null);

            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.Login(new LoginRequest
            {
                Email = "x",
                Password = "y"
            });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Login_Valid_ReturnsToken()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetUserByEmailAndPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new User { Id = 1, Email = "a", Role = "User" });

            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.Login(new LoginRequest
            {
                Email = "a",
                Password = "b"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // ===== CONFIRM RESET =====

        [Fact]
        public void ConfirmReset_Invalid_ReturnsBadRequest()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.CheckResetCode(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            var email = new Mock<IEmailService>();

            var controller = CreateController(repo, email);

            var result = controller.ConfirmResetCode(new ResetPasswordConfirmRequest
            {
                Email = "x",
                Code = "y"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
