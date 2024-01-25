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

namespace Iotbcdg.Functions
{
    public static class DeviceFunc
    {
        [FunctionName("DeviceFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "device")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Devices HTTP trigger function processed a request.");
            AppUser user = await AuthHandler.CheckIfUserExists(req);
            if (user == null)
                return new UnauthorizedObjectResult("User does not exist. Try login first.");

            string serializedData = JsonConvert.SerializeObject(user.Devices);
            string encryptionKey = Environment.GetEnvironmentVariable("EncryptionSymetricKey", EnvironmentVariableTarget.Process);
            string encryptedData = Encryption.EncryptData(encryptionKey, serializedData);
            return new OkObjectResult(encryptedData);
        }
    }
}
