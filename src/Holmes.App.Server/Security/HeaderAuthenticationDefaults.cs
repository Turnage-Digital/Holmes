namespace Holmes.App.Server.Security;

public static class HeaderAuthenticationDefaults
{
    public const string Scheme = "Header";

    public static class Headers
    {
        public const string Issuer = "X-Auth-Issuer";
        public const string Subject = "X-Auth-Subject";
        public const string Email = "X-Auth-Email";
        public const string Name = "X-Auth-Name";
        public const string AuthenticationMethod = "X-Auth-Amr";
    }
}