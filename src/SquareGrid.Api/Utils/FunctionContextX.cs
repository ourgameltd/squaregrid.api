using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using SquareGrid.Common.Models;
using System.Security.Claims;
using System.Text;

namespace SquareGrid.Api.Utils
{
    public static class FunctionContextX
    {
        public static async Task<User> GetUser(this FunctionContext ctx)
        {
            var user = await ctx.GetUserIfPopulated();

            if (user == null)
            {
                throw new ArgumentNullException();
            }

            return user;
        }


        public static async Task<User?> GetUserIfPopulated(this FunctionContext ctx)
        {
            var req = await ctx.GetHttpRequestDataAsync();

            if (req == null)
            {
                return null;
            }

            string? json = null;

            if (req.Headers.TryGetValues("x-ms-client-principal", out var header))
            {
                var data = header.First();
                var decoded = Convert.FromBase64String(data);
                json = Encoding.UTF8.GetString(decoded);

#if DEBUG
                // ML: Mother of all hacks while this bug gets fixed
                // https://github.com/Azure/static-web-apps/issues/897

                if (string.IsNullOrWhiteSpace(json) && req.Cookies.Any(i => i.Name == "StaticWebAppsAuthCookie"))
                {
                    data = req.Cookies.First(i => i.Name == "StaticWebAppsAuthCookie").Value;
                    decoded = Convert.FromBase64String(data);
                    json = Encoding.UTF8.GetString(decoded);
                }
#endif
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            AuthenticatedUser? principal = JsonConvert.DeserializeObject<AuthenticatedUser?>(json);

            if (principal == null)
            {
                return null;
            }

            return new User(principal);
        }
    }
}
