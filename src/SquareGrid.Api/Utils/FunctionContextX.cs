using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SquareGrid.Common.Models;
using System.Text;

namespace SquareGrid.Api.Utils
{
    public static class FunctionContextX
    {
        public static User GetUser(this HttpRequestData req, ILogger? logger = null)
        {
            var user = req.GetUserIfPopulated(logger);

            if (user == null)
            {
                throw new ArgumentNullException();
            }

            return user;
        }


        public static User? GetUserIfPopulated(this HttpRequestData req, ILogger? logger = null)
        {
            string? json = null;

            if (req.Headers.TryGetValues("x-ms-client-principal", out var header))
            {
                var data = header.First();
                var decoded = Convert.FromBase64String(data);
                json = Encoding.UTF8.GetString(decoded);

#if DEBUG
                // ML: Mother of all hacks while this bug gets fixed
                // https://github.com/Azure/static-web-apps/issues/897

                if (string.IsNullOrWhiteSpace(json))
                {
                    if (req.Cookies.Any(i => i.Name == "StaticWebAppsAuthCookie"))
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
                }
#endif
            }
            else
            {
                logger?.LogInformation("Unable to get x-ms-client-principal header.");
                logger?.LogInformation("Headers passed.");
                foreach (var item in req.Headers)
                {
                    logger?.LogInformation(item.Key);
                }
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                logger?.LogInformation("No JSON found in cookie or header.");
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
