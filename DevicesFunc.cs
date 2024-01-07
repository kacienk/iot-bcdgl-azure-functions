using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Iotbcdg.Model;
using Iotbcdg.Auth;

namespace Iotbcdg.Functions
{
    public static class DevicesFunc
    {
        [FunctionName("DevicesFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Devices HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            return new OkObjectResult(user.Devices);
        }
    }
}
