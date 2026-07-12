using backend.Auth;
using backend.Models;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace backend.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly AuthOptions _options;

        public JwtTokenService(IOptions<AuthOptions> options)
        {
            _options = options.Value;
        }

        public AuthResponse CreateAccessToken(ApplicationUser user)
        {
            ValidateOptions();

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(_options.AccessTokenExpirationMinutes);

            var header = new Dictionary<string, object>
            {
                ["alg"] = "HS256",
                ["typ"] = "JWT"
            };

            var payload = new Dictionary<string, object>
            {
                ["iss"] = _options.Issuer,
                ["aud"] = _options.Audience,
                ["sub"] = user.Id.ToString(),
                ["name"] = user.Name,
                ["email"] = user.Email,
                ["role"] = user.Role,
                ["jti"] = Guid.NewGuid().ToString("N"),
                ["iat"] = now.ToUnixTimeSeconds(),
                ["nbf"] = now.ToUnixTimeSeconds(),
                ["exp"] = expiresAt.ToUnixTimeSeconds()
            };

            var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
            var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
            var unsignedToken = $"{encodedHeader}.{encodedPayload}";
            var signature = Base64UrlEncode(Sign(unsignedToken));

            return new AuthResponse
            {
                AccessToken = $"{unsignedToken}.{signature}",
                ExpiresAt = expiresAt,
                User = UserMapper.ToResponse(user)
            };
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            ValidateOptions();

            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var unsignedToken = $"{parts[0]}.{parts[1]}";
            var expectedSignature = Base64UrlEncode(Sign(unsignedToken));

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(parts[2]),
                Encoding.ASCII.GetBytes(expectedSignature)))
            {
                return null;
            }

            try
            {
                var header = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64UrlDecode(parts[0]));
                if (header is null ||
                    !header.TryGetValue("alg", out var algorithm) ||
                    algorithm.GetString() != "HS256")
                {
                    return null;
                }

                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64UrlDecode(parts[1]));
                if (payload is null)
                {
                    return null;
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var issuer = GetString(payload, "iss");
                var audience = GetString(payload, "aud");
                var subject = GetString(payload, "sub");
                var name = GetString(payload, "name");
                var email = GetString(payload, "email");
                var role = GetString(payload, "role");
                var expiresAt = GetInt64(payload, "exp");
                var notBefore = GetInt64(payload, "nbf");

                if (issuer != _options.Issuer ||
                    audience != _options.Audience ||
                    string.IsNullOrWhiteSpace(subject) ||
                    string.IsNullOrWhiteSpace(role) ||
                    expiresAt <= now ||
                    notBefore > now)
                {
                    return null;
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, subject),
                    new(ClaimTypes.Name, name ?? subject),
                    new(ClaimTypes.Email, email ?? string.Empty),
                    new(ClaimTypes.Role, role),
                    new("sub", subject),
                    new("role", role)
                };

                var identity = new ClaimsIdentity(claims, HolloJwtDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                return new ClaimsPrincipal(identity);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private byte[] Sign(string value)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
            return hmac.ComputeHash(Encoding.ASCII.GetBytes(value));
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
            {
                throw new InvalidOperationException("Auth signing key must have at least 32 bytes.");
            }

            if (_options.AccessTokenExpirationMinutes <= 0)
            {
                throw new InvalidOperationException("Auth access token expiration must be greater than zero.");
            }
        }

        private static string? GetString(Dictionary<string, JsonElement> payload, string key)
        {
            return payload.TryGetValue(key, out var value) ? value.GetString() : null;
        }

        private static long GetInt64(Dictionary<string, JsonElement> payload, string key)
        {
            return payload.TryGetValue(key, out var value) && value.TryGetInt64(out var result) ? result : 0;
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            var padding = base64.Length % 4;

            if (padding > 0)
            {
                base64 = base64.PadRight(base64.Length + (4 - padding), '=');
            }

            return Convert.FromBase64String(base64);
        }
    }
}
