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

namespace Iotbcdg.Funcions
{
    public static class confirm_add
    {
        [FunctionName("confirm_add")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Confirm add HTTP trigger function processed a request.");

            string connectionString = Environment.GetEnvironmentVariable("IoTHubConnection", EnvironmentVariableTarget.Process);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            bool foundDevice = await CheckDeviceExistsAsync(connectionString, data);

            return new OkObjectResult(responseMessage);
        }

        static async Task<bool> CheckDeviceExistsAsync(ILogger log, string connectionString, dynamic deviceData)
        {
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
                Console.WriteLine($"Error checking device existence: {ex.Message}");
                return false;
            }
        }

        static void AddDeviceToUserInCosmosDB()
        {

        }

        static async Task VerifyUserAndAddDeviceAsync(string userId, string deviceId)
        {
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);
            using (var cosmosClient = new CosmosClient(cosmosConnection))
            {
                var database = cosmosClient.GetDatabase(databaseId);
                var container = database.GetContainer(containerId);

                dynamic user = await GetUserByIdAsync(container, userId);

                if (user != null)
                {
                    Console.WriteLine($"User with ID '{userId}' found in Cosmos DB.");
                    Console.WriteLine($"Adding device '{deviceId}' to user's devices.");

                    // Add the device to the devices array
                    user.Devices.Add(deviceId);

                    // Update the user document in Cosmos DB
                    await UpdateUserAsync(container, user);

                    Console.WriteLine("Device added successfully.");
                }
                else
                {
                    Console.WriteLine($"User with ID '{userId}' not found in Cosmos DB.");
                }
            }
        }

        static async Task<User> GetUserByIdAsync(Container container, string userId)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.id = @userId")
                .WithParameter("@userId", userId);

            var iterator = container.GetItemQueryIterator<User>(query);
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        static async Task UpdateUserAsync(Container container, User user)
        {
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.Id));
            Console.WriteLine($"Update status: {response.StatusCode}");
        }
    }
}
