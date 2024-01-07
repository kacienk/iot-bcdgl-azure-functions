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
using Iotbcdg.Auth;
using System.Web.Http;

namespace Iotbcdg.Functions
{
    public static class ConfirmAddFunc
    {
        [FunctionName("ConfirmAddFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "confirm-add")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Confirm add HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            bool foundDevice = await CheckDeviceExistsAsync(log, data);
            if (!foundDevice)
                return new NotFoundObjectResult("Device does not exist");

            bool updatedUser = await AddDeviceToUserAsync(log, user, data.deviceId);

            return updatedUser
                ? new OkObjectResult("Added device")
                : new InternalServerErrorResult();
        }

        static async Task<bool> CheckDeviceExistsAsync(ILogger log, dynamic deviceData)
        {
            string connectionString = Environment.GetEnvironmentVariable("IoTHubConnection", EnvironmentVariableTarget.Process);
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            try
            {
                var device = await registryManager.GetDeviceAsync(deviceData.deviceId);
                if (device != null)
                {
                    log.LogInformation($"Device with ID '{deviceData.deviceId}' found in IoT Hub.");
                    log.LogInformation($"Device details: {device}");
                    return true;
                }
                else
                {
                    log.LogInformation($"Device with ID '{deviceData.deviceId}' not found in IoT Hub.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error checking device existence: {ex.Message}");
                return false;
            }
        }

        static async Task<bool> AddDeviceToUserAsync(ILogger log, AppUser user, string deviceId)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);

            log.LogInformation($"Pairing device '{deviceId}' with user '{user.Id}' devices.");
            user.Devices.Add(deviceId);
            var response = await AppUser.UpdateUserAsync(container, user, log);
            if (((int)response.StatusCode) >= 200 && ((int)response.StatusCode) < 300)
            {
                log.LogInformation("Device added successfully.");
                return true;
            }
            else
            {
                log.LogInformation("Device could not be added.");
                return false;
            }
        }
    }
}
