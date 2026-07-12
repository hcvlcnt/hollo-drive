using backend.Services;
using backend.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace backend.Auth
{
    public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string AccessTokenCookieName = "hollo_access_token";

        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;

        public JwtAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ITokenService tokenService,
            IUserRepository userRepository)
            : base(options, logger, encoder)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers.Authorization.ToString();
            var token = Request.Cookies[AccessTokenCookieName];

            if (!string.IsNullOrWhiteSpace(authorization))
            {
                const string bearerPrefix = "Bearer ";
                if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return AuthenticateResult.Fail("Invalid authorization header.");
                }

                token = authorization[bearerPrefix.Length..].Trim();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return AuthenticateResult.NoResult();
            }

            var principal = _tokenService.ValidateToken(token);

            if (principal is null)
            {
                return AuthenticateResult.Fail("Invalid token.");
            }

            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return AuthenticateResult.Fail("Invalid user claim.");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken: Context.RequestAborted);
            if (user is null || !user.IsActive)
            {
                return AuthenticateResult.Fail("User is inactive or deleted.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("sub", user.Id.ToString()),
                new("role", user.Role)
            };
            var identity = new ClaimsIdentity(claims, HolloJwtDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
