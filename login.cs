using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Iotbcdg.Auth;
using Microsoft.Extensions.Primitives;
using Iotbcdg.Model;
using Microsoft.Azure.Cosmos;
using System.Runtime.ExceptionServices;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Iotbcdg.Functions
{
    public static class login
    {
        [FunctionName("login")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Login HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CreateUserIfNotExists(req, log);

            if (user != null)
            {
                log.LogInformation($"User logged in: User ID: {user.Id}");
                return new OkObjectResult($"User logged in: User ID: {user.Id}");
            }
            else
            {
                return new UnauthorizedResult();
            }
        }
    }
}
