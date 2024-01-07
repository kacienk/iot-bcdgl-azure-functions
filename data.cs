using System.Runtime.CompilerServices;
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
using Microsoft.Azure.Amqp.Framing;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Cosmos;

namespace Iotbcdg.Functions
{
    public static class data
    {
        [FunctionName("data")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Data HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            Dictionary<string, string> queryParams = new(req.GetQueryParameterDictionary());
            if (!queryParams.ContainsKey("deviceId"))
            {
                log.LogWarning("Query with no deviceId received.");
                return new BadRequestObjectResult("No deviceId in query params");
            }

            string deviceId = queryParams["deviceId"];
            if (!user.Devices.Contains(deviceId))
            {
                log.LogWarning("User tried to get access to either not existing or not paired device.");
                return new UnauthorizedObjectResult("Either device does not exist or user is not paired with device");
            }

            List<DeviceData> deviceData = await GetDeviceDataAsync(deviceId);
            return new OkObjectResult(deviceData);
        }

        static async Task<List<DeviceData>> GetDeviceDataAsync(string deviceId)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);

            return await DeviceData.GetDeviceDataAsync(container, deviceId);
        }
    }
}
