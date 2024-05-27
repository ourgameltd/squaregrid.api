using System.Security.Claims;

namespace SquareGrid.Common.Models
{
    public class User
    {
        public readonly ClaimsPrincipal Principal;

        public User(ClaimsPrincipal principal)
        {
            Principal = principal;
        }

        public bool Authenticated => Principal.Identity?.IsAuthenticated ?? false;

        public string Name => Principal.FindFirst("name")?.Value ?? string.Empty;

        public string Email => Principal.FindFirst("emails")?.Value ?? string.Empty;

        public string ObjectId => Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
    }
}
