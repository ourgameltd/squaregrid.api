using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SquareGrid.Common.Models;
using System.Security.Claims;
using System.Text;

namespace SquareGrid.Api.Utils
{
    public static class FunctionContextX
    {
        public static async Task<User> GetUser(this FunctionContext ctx, ILogger? logger = null)
        {
            var user = await ctx.GetUserIfPopulated(logger);

            if (user == null)
            {
                throw new ArgumentNullException();
            }

            return user;
        }


        public static async Task<User?> GetUserIfPopulated(this FunctionContext ctx, ILogger? logger = null)
        {
            var req = await ctx.GetHttpRequestDataAsync();

            if (req == null)
            {
                logger?.LogInformation("Unable to get request data");
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
                else
                {
                    logger?.LogInformation("Unable to get StaticWebAppsAuthCookie cookie.");
                    return null;
                }
#endif
            }
            else
            {
                logger?.LogInformation("Unable to get x-ms-client-principal header.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                logger?.LogInformation("No JSON found in cookie or header.");
                logger?.LogInformation("Headers passed.");
                foreach (var item in req.Headers)
                {
                    logger?.LogInformation(item.Key);
                }
                return null;
            }


            logger?.LogInformation("Got JSON. " + json);

            AuthenticatedUser? principal = JsonConvert.DeserializeObject<AuthenticatedUser?>(json);

            if (principal == null)
            {
                logger?.LogInformation("Pricipal is null.");
                return null;
            }

            return new User(principal);
        }
    }
}
