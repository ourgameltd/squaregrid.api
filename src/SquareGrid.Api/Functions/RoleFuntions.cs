using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace AgkEnergyTransition.Api
{
    public class RoleFunctions
    {
        private readonly ILogger<RoleFunctions> logger;

        public RoleFunctions(ILogger<RoleFunctions> logger)
        {
            this.logger = logger;
        }

        [FunctionName("GetRoles")]
        public IActionResult GetRoles(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetRoles")] HttpRequest req)
        {
            List<string> roles = new List<string>();

            // Get additional roles

            return new JsonResult(roles.ToArray());
        }
    }
}