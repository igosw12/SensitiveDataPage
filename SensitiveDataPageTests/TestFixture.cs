using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SensitiveDataPageTests
{
    public class TestFixture
    {
        public static string HashPassword(string password)
        {
            var salt = new byte[128 / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            return Convert.ToBase64String(salt) + ":" + hash;
        }

        public HttpContext CreateMockedHttpContext()
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            return new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
        }

        public HttpContext CreateAuthenticatedHttpContext(Guid userId, string email)
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object,
                User = principal
            };
        }

        public IUrlHelper CreateMockedUrlHelper()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .SetupGet(u => u.ActionContext)
                .Returns(actionContext);
            urlHelperMock
                .Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns("https://localhost/test");
            return urlHelperMock.Object;
        }
    }
}
