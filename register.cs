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
using Microsoft.Azure.Cosmos;
using Iotbcdg.Exceptions;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Iotbcdg.Functions
{
    public static class register
    {
        [FunctionName("register")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Register HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // email, password
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            try
            {
                await AddNewUser(log, data);
                return new OkObjectResult(new { body = "success" });
            }
            catch (EntryAlreadyExists e)
            {
                log.LogError(e.Message);
                return new ObjectResult(new { body = "already exists" }) { StatusCode = 403 };
            }
        }

        private static async Task AddNewUser(ILogger log, dynamic userData)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);
            string pepper = Environment.GetEnvironmentVariable("Pepper", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var database = cosmosClient.GetDatabase(databaseId);
            var container = database.GetContainer(containerId);

            dynamic user = await AppUser.GetUserByEmailAsync(container, userData.email);
            if (user != null)
                throw new EntryAlreadyExists($"Tried to add user with email '{userData.email}' but user with this email already exists.");

            await AppUser.CreateUserAsync(container, userData, pepper, log);
        }
    }
}
