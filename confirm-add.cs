using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Cosmos;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Iotbcdg.Model;

namespace Iotbcdg.Functions
{
    public static class confirm_add
    {
        [FunctionName("confirm_add")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Confirm add HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            bool foundDevice = await CheckDeviceExistsAsync(log, data);
            bool foundUser = await VerifyUserAndAddDeviceAsync(log, data.userId, data.id);

            if (foundDevice && foundUser)
                return new OkObjectResult(new { body = "success" });

            return foundUser
                ? new UnauthorizedObjectResult(new { body = "user not found" })
                : new NotFoundObjectResult(new { body = "device not found" });
        }

        static async Task<bool> CheckDeviceExistsAsync(ILogger log, dynamic deviceData)
        {
            string connectionString = Environment.GetEnvironmentVariable("IoTHubConnection", EnvironmentVariableTarget.Process);
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            try
            {
                var device = await registryManager.GetDeviceAsync(deviceData.id);
                if (device != null)
                {
                    log.LogInformation($"Device with ID '{deviceData.id}' found in IoT Hub.");
                    log.LogInformation($"Device details: {device}");
                    return true;
                }
                else
                {
                    log.LogInformation($"Device with ID '{deviceData.id}' not found in IoT Hub.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error checking device existence: {ex.Message}");
                return false;
            }
        }

        static async Task<bool> VerifyUserAndAddDeviceAsync(ILogger log, string userId, string deviceId)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);
            using var cosmosClient = new CosmosClient(cosmosConnection);
            var database = cosmosClient.GetDatabase(databaseId);
            var container = database.GetContainer(containerId);

            dynamic user = await AppUser.GetUserByIdAsync(container, userId);

            if (user != null)
            {
                log.LogInformation($"User with ID '{userId}' found in Cosmos DB.");
                log.LogInformation($"Adding device '{deviceId}' to user's devices.");

                user.Devices.Add(deviceId);
                await AppUser.UpdateUserAsync(container, user, log);
                log.LogInformation("Device added successfully.");
                return true;
            }
            else
            {
                log.LogInformation($"User with ID '{userId}' not found in Cosmos DB.");
                return false;
            }
        }
    }
}
