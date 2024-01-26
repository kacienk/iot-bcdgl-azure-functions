using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Iotbcdg.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Iotbcdg.Auth
{
    public class AuthHandler
    {
        public static async Task<GoogleUserData> GetGoogleUserInfoAsync(string accessToken, ILogger log = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
            log?.LogInformation(await response.Content.ReadAsStringAsync());
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var userInfo = JsonConvert.DeserializeObject<GoogleUserData>(responseContent);

                return userInfo;
            }
            else
            {
                return null;
            }
        }

        public static async Task<AppUser> CheckIfUserExists(HttpRequest req)
        {
            if (!req.Headers.TryGetValue("Authorization", out StringValues authHeader))
                return null;

            string accessToken = authHeader.ToString().Replace("Bearer ", "");
            GoogleUserData userInfo = await GetGoogleUserInfoAsync(accessToken);

            if (userInfo == null)
                return null;

            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);
            AppUser user = await AppUser.GetUserByIdAsync(container, userInfo.sub);

            return user;
        }

        public static async Task<AppUser> CreateUserIfNotExists(HttpRequest req)
        {
            if (!req.Headers.TryGetValue("Authorization", out StringValues authHeader))
                return null;

            string accessToken = authHeader.ToString().Replace("Bearer ", "");
            dynamic userInfo = await GetGoogleUserInfoAsync(accessToken);

            if (userInfo == null)
                return null;

            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);

            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);
            AppUser user = await AppUser.GetUserByIdAsync(container, userInfo.sub);

            if (user != null)
                return user;

            ItemResponse<AppUser> response = await AppUser.CreateUserAsync(container, userInfo.sub);
            return response.Resource;
        }

        public static async Task<AppUser> CreateUserIfNotExists(HttpRequest req, ILogger log)
        {
            if (!req.Headers.TryGetValue("Authorization", out StringValues authHeader))
                return null;
            log.LogInformation(authHeader);
            log.LogInformation(authHeader.GetType().Name);
            log.LogInformation(authHeader.ToString().GetType().Name);

            string accessToken = authHeader.ToString().Replace("Bearer ", "");
            GoogleUserData userInfo = await GetGoogleUserInfoAsync(accessToken, log);
            log.LogInformation(userInfo.sub);

            if (userInfo == null)
                return null;

            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosConnection", EnvironmentVariableTarget.Process);
            string databaseId = Environment.GetEnvironmentVariable("DatabaseID", EnvironmentVariableTarget.Process);
            string containerId = Environment.GetEnvironmentVariable("UserContainerID", EnvironmentVariableTarget.Process);
            using var cosmosClient = new CosmosClient(cosmosConnection);
            var container = cosmosClient.GetContainer(databaseId, containerId);
            AppUser user = await AppUser.GetUserByIdAsync(container, userInfo.sub);
            if (user != null)
                return user;

            ItemResponse<AppUser> response = await AppUser.CreateUserAsync(container, userInfo, log);
            return response.Resource;
        }
    }
}