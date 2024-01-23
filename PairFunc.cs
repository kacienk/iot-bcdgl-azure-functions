using System.Diagnostics.Contracts;
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
using Iotbcdg.Model;
using Iotbcdg.Auth;
using Microsoft.Azure.Cosmos;
using System.Web.Http;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Iotbcdg.Functions
{
    public static class PairFunc
    {
        [FunctionName("PairFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pair")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Pair HTTP trigger function processed a request.");
            PairData pairData;
            try
            {
                pairData = await ProcessRequestBodyAsync(req);
            }
            catch (JsonSerializationException)
            {
                log.LogError("Failed to process request body. Either format is invalid or unathorized access.");
                return new UnauthorizedResult();
            }

            if (pairData == null)
                return new BadRequestObjectResult("invalid data");

            bool foundDevice = await CheckDeviceExistsAsync(log, pairData.DeviceId);
            if (!foundDevice)
                return new NotFoundObjectResult("Device does not exist");

            if (pairData.RequestType == "app")
            {
                return await ProcessAppRequest(req, log, pairData);
            }
            else
            {
                return await ProcessDeviceRequest(log, pairData);
            }
        }

        static async Task<PairData> ProcessRequestBodyAsync(HttpRequest req)
        {
            string encryptionKey = Environment.GetEnvironmentVariable("EncryptionSymetricKey", EnvironmentVariableTarget.Process);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string decryptedBody = DecryptData(encryptionKey, requestBody);

            return JsonConvert.DeserializeObject<PairData>(decryptedBody);
        }

        static async Task<IActionResult> ProcessAppRequest(HttpRequest req, ILogger log, PairData pairData)
        {
            log.LogInformation("Processing app pair request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            Container container = GetContainer("CosmosConnection", "DatabaseID", "PairQueueContainerID");
            PairData correspondingPairData = new()
            {
                DeviceId = pairData.DeviceId,
                RequestType = "device"
            };
            PairDbEntry correspondingPairDbEntry = await FindPairDbEntry(container, correspondingPairData);

            if (correspondingPairDbEntry != null && correspondingPairDbEntry.DeviceId != null)
            {
                log.LogInformation("Found corresponding pair db entry.");
                bool udateResult = await AddDeviceToUserAsync(log, user, pairData.DeviceId);
                return udateResult
                    ? new OkObjectResult("device properly added to user")
                    : new BadRequestObjectResult("could not add device");
            }

            log.LogInformation($"Created pairing request for user '{user.UserId}' with.");
            var savedEntry = await CreatePairDbEntry(container, pairData, userId: user.UserId);
            return savedEntry != null ? new OkResult() : new BadRequestResult();
        }

        static async Task<IActionResult> ProcessDeviceRequest(ILogger log, PairData pairData)
        {
            log.LogInformation("Processing device pair request.");
            Container container = GetContainer("CosmosConnection", "DatabaseID", "PairQueueContainerID");
            PairData correspondingPairData = new()
            {
                DeviceId = pairData.DeviceId,
                RequestType = "app"
            };
            PairDbEntry correspondingPairDbEntry = await FindPairDbEntry(container, correspondingPairData);

            if (correspondingPairDbEntry != null && correspondingPairDbEntry.UserId != null)
            {
                log.LogInformation("Found corresponding pair db entry.");
                Container usersContainer = GetContainer("CosmosConnection", "DatabaseID", "UserContainerID");
                var user = await AppUser.GetUserByIdAsync(usersContainer, correspondingPairDbEntry.UserId);
                if (user == null)
                {
                    log.LogError("User id is corrupted.");
                    return new NotFoundObjectResult("user in pair db entry not found");
                }

                bool udateResult = await AddDeviceToUserAsync(log, user, pairData.DeviceId);
                return udateResult
                    ? new OkObjectResult("device properly added to user")
                    : new BadRequestObjectResult("could not add device");
            }

            log.LogInformation($"Created pairing request for device '{pairData.DeviceId}'.");
            var savedEntry = await CreatePairDbEntry(container, pairData);
            return savedEntry != null ? new OkResult() : new BadRequestResult();
        }

        static async Task<PairDbEntry> CreatePairDbEntry(Container container, PairData pairData, string userId = null)
        {
            var dbEntry = new PairDbEntry
            {
                RequestType = pairData.RequestType,
                DeviceId = pairData.DeviceId,
                Timestamp = DateTime.Now,
                UserId = userId
            };

            return await container.CreateItemAsync(dbEntry, new PartitionKey(dbEntry.Timestamp.ToString()));
        }

        static async Task<PairDbEntry> FindPairDbEntry(Container container, PairData pairData)
        {
            var query = new QueryDefinition($"SELECT TOP 1 * FROM c WHERE c.DeviceId = @DeviceId AND c.Timestamp <= @Timestamp AND c.RequestType = @RequestType ORDER BY c.Timestamp DESC")
                .WithParameter("@DeviceId", pairData.DeviceId)
                .WithParameter("@Timestamp", DateTime.UtcNow.AddMinutes(-3))
                .WithParameter("@RequestType", pairData.RequestType);

            var iterator = container.GetItemQueryIterator<PairDbEntry>(query);
            var pairDbEntry = await iterator.ReadNextAsync();

            return pairDbEntry.FirstOrDefault();
        }

        static async Task<bool> CheckDeviceExistsAsync(ILogger log, string deviceId)
        {
            string connectionString = Environment.GetEnvironmentVariable("IoTHubConnection", EnvironmentVariableTarget.Process);
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            try
            {
                var device = await registryManager.GetDeviceAsync(deviceId);
                if (device != null)
                {
                    log.LogInformation($"Device with ID '{deviceId}' found in IoT Hub.");
                    log.LogInformation($"Device details: {device}");
                    return true;
                }
                else
                {
                    log.LogInformation($"Device with ID '{deviceId}' not found in IoT Hub.");
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
            var container = GetContainer("CosmosConnection", "DatabaseID", "UserContainerID");

            log.LogInformation($"Pairing device '{deviceId}' with user '{user.UserId}' devices.");
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

        static string DecryptData(string key, string encryptedString)
        {
            byte[] iv = Convert.FromBase64String(encryptedString[..16]);
            byte[] buffer = Convert.FromBase64String(encryptedString[16..]);

            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream memoryStream = new(buffer);
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);
            return streamReader.ReadToEnd();
        }

        static Container GetContainer(string cosmosConnection, string dbId, string containerId)
        {
            string cosmosConnectionEnvVar = Environment.GetEnvironmentVariable(cosmosConnection, EnvironmentVariableTarget.Process);
            string databaseIdEnvVar = Environment.GetEnvironmentVariable(dbId, EnvironmentVariableTarget.Process);
            string containerIdEnvVar = Environment.GetEnvironmentVariable(containerId, EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnectionEnvVar);
            return cosmosClient.GetContainer(databaseIdEnvVar, containerIdEnvVar);
        }
    }
}
