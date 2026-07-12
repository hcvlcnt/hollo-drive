namespace backend.Auth
{
    public class AdminUserOptions
    {
        public const string SectionName = "AdminUser";

        public string Name { get; set; } = "Admin";
        public string Email { get; set; } = "admin@hollo.local";
        public string Password { get; set; } = "Admin@123456";
    }
}
