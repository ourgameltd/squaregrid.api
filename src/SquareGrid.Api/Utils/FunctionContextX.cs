using Microsoft.Azure.Functions.Worker;
using SquareGrid.Common.Models;
using System.Security.Claims;

namespace SquareGrid.Api.Utils
{
    public static class FunctionContextX
    {
        public static User GetUser(this FunctionContext ctx, bool forceAuthenticated = true)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException();
            }

            if (!ctx.Items.TryGetValue(nameof(ClaimsPrincipal), out object? princpal))
            {
                throw new InvalidDataException("No user data available in cache.");
            }

            var claimsPrincipal = princpal as ClaimsPrincipal;

            if (claimsPrincipal == null)
            {
                throw new InvalidDataException("No user data available in cache for boxed item.");
            }

            if (forceAuthenticated && claimsPrincipal.Identity?.IsAuthenticated == false)
            {
                throw new InvalidDataException("Claims principal is valid but must be authenticated.");
            }

            return new User(claimsPrincipal);
        }


        public static User? GetUserIfPopulated(this FunctionContext ctx, bool forceAuthenticated = true)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException();
            }

            if (!ctx.Items.TryGetValue(nameof(ClaimsPrincipal), out object? princpal))
            {
                return null;
            }

            var claimsPrincipal = princpal as ClaimsPrincipal;

            if (claimsPrincipal == null)
            {
                return null;
            }

            if (forceAuthenticated && claimsPrincipal.Identity?.IsAuthenticated == false)
            {
                return null;
            }

            return new User(claimsPrincipal);
        }
    }
}
