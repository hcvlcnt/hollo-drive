namespace backend.Models
{
    public static class ApplicationRole
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static bool IsValid(string role)
        {
            return string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, User, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string role)
        {
            if (string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Admin;
            }

            if (string.Equals(role, User, StringComparison.OrdinalIgnoreCase))
            {
                return User;
            }

            throw new ArgumentException("Invalid role.", nameof(role));
        }
    }
}
