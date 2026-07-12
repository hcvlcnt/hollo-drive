using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string AccessTokenCookieName = "hollo_access_token";

        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IAuthService authService, IWebHostEnvironment environment)
        {
            _authService = authService;
            _environment = environment;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authService.RegisterAsync(request, cancellationToken);
                SetAccessTokenCookie(response);

                return Created("/api/auth/me", ToPublicAuthResponse(response));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(request, cancellationToken);

            if (response is null)
            {
                return Unauthorized(new { error = "Invalid email or password." });
            }

            SetAccessTokenCookie(response);

            return Ok(ToPublicAuthResponse(response));
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AccessTokenCookieName, CreateCookieOptions());

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserAsync(userId, cancellationToken);
            return user is null ? Unauthorized() : Ok(user);
        }

        private void SetAccessTokenCookie(AuthResponse response)
        {
            var options = CreateCookieOptions();
            options.Expires = response.ExpiresAt;
            Response.Cookies.Append(AccessTokenCookieName, response.AccessToken, options);
        }

        private CookieOptions CreateCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = !_environment.IsDevelopment(),
                Path = "/api"
            };
        }

        private static object ToPublicAuthResponse(AuthResponse response)
        {
            return new
            {
                response.ExpiresAt,
                response.User
            };
        }
    }
}
