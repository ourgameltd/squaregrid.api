using System.Security.Claims;

namespace SquareGrid.Common.Models
{
    public class User
    {
        public readonly AuthenticatedUser Principal;

        public User(AuthenticatedUser principal)
        {
            Principal = principal;
        }

        public bool Authenticated => Principal != null;

        public string Name => Principal.UserDetails;

        public string ObjectId => Principal.UserId.ToString().ToLower();
    }

    public class AuthenticatedUser
    {
        public required string IdentityProvider { get; set; }

        public required Guid UserId { get; set; }

        public string Name => Claims.FirstOrDefault(i => i.Typ == "name")?.Val ?? "Unknown";

        public string? Email => Claims.FirstOrDefault(i => i.Typ == "preferred_username")?.Val;

        public required string UserDetails { get; set; }

        public IEnumerable<string>? UserRoles { get; set; }

        public required IEnumerable<AuthenticatedUserClaim> Claims { get; set; }
    }

    public class AuthenticatedUserClaim
    {
        public required string Typ { get; set; }

        public required string Val { get; set; }
    }
}
