namespace backend.Auth
{
    public class AuthOptions
    {
        public const string SectionName = "Auth";

        public string Issuer { get; set; } = "hollo";
        public string Audience { get; set; } = "hollo-api";
        public string SigningKey { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 120;
    }
}
