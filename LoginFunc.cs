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
    public static class LoginFunc
    {
        [FunctionName("LoginFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "login")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Login aaaaaaaa HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CreateUserIfNotExists(req, log);

            if (user != null)
            {
                log.LogInformation($"User logged in: User ID: {user.UserId}");
                return new OkObjectResult($"User logged in: User ID: {user.UserId}");
            }
            else
            {
                return new UnauthorizedResult();
            }
        }
    }
}
