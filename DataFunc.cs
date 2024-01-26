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
    public static class DataFunc
    {
        [FunctionName("DataFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            log.LogInformation("Data HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            if (!user.Devices.Contains(id))
            {
                log.LogWarning($"User '{user.UserId}' tried to get access to either not existing or not paired device '{id}'.");
                return new UnauthorizedObjectResult($"user is not paired with device {id}");
            }

            List<DeviceData> deviceData = await GetDeviceDataAsync(id);
            log.LogInformation($"Logs count: {deviceData.Count}");
            string serializedData = JsonConvert.SerializeObject(deviceData);
            log.LogInformation(serializedData);

            string encryptionKey = Environment.GetEnvironmentVariable("EncryptionSymetricKey", EnvironmentVariableTarget.Process);
            string encryptedData = EncryptionHandler.EncryptData(encryptionKey, serializedData);
            return new OkObjectResult(encryptedData);
        }

        static async Task<List<DeviceData>> GetDeviceDataAsync(string deviceId)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("DataContainerID", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);

            List<DataDbEntry> entries = await DataDbEntry.GetDataDbEntryAsync(container, deviceId);
            return DeviceData.ParseDataDbEntreis(entries);
        }
    }
}
