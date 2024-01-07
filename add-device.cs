using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Iotbcdg.Model;
using Iotbcdg.Auth;

namespace Iotbcdg.Functions
{
    public static class add_device
    {
        [FunctionName("add_device")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
        {
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            var deviceInfo = new
            {
                DeviceId = Guid.NewGuid().ToString(),
                PrimaryKey = GenerateSecureRandomKey(),
                SecondaryKey = GenerateSecureRandomKey(),
            };
            RegisterDeviceInIoTHub(deviceInfo);

            var response = new
            {
                deviceInfo.DeviceId,
                deviceInfo.PrimaryKey,
                deviceInfo.SecondaryKey,
                IoTHubDeviceConnection = Environment.GetEnvironmentVariable("IoTHubConnectionDevice", EnvironmentVariableTarget.Process)
            };
            return new OkObjectResult(JsonConvert.SerializeObject(response));
        }

        private static void RegisterDeviceInIoTHub(dynamic deviceInfo)
        {
            string iotHubConnectionString = Environment.GetEnvironmentVariable("IoTHubConnection", EnvironmentVariableTarget.Process);
            var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);

            var device = new Device(deviceInfo.DeviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.Sas,
                    SymmetricKey = new SymmetricKey
                    {
                        PrimaryKey = deviceInfo.PrimaryKey,
                        SecondaryKey = deviceInfo.SecondaryKey
                    }
                }
            };

            registryManager.AddDeviceAsync(device).Wait();
            Console.WriteLine($"Device registered: {deviceInfo.DeviceId}");
        }

        private static string GenerateSecureRandomKey()
        {
            byte[] keyBytes = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            return Convert.ToBase64String(keyBytes);
        }
    }
}
