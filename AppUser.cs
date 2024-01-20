using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iotbcdg.Auth;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Iotbcdg.Model
{
    public class AppUser
    {
        public string UserId { get; set; }
        public List<string> Devices { get; set; } = new List<string>();

        public static async Task<AppUser> GetUserByIdAsync(Container container, string id)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.id = @userId")
                .WithParameter("@userId", id);

            var iterator = container.GetItemQueryIterator<AppUser>(query);
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        public static async Task<AppUser> GetUserByTokenAsync(Container container, string accessToken)
        {
            dynamic userInfo = await AuthHandler.GetGoogleUserInfoAsync(accessToken);

            return userInfo != null
                ? await AppUser.GetUserByIdAsync(container, userInfo.sub)
                : null;

        }

        public static async Task<ItemResponse<AppUser>> UpdateUserAsync(Container container, AppUser user)
        {
            return await container.UpsertItemAsync(user, new PartitionKey(user.UserId));
        }

        public static async Task<ItemResponse<AppUser>> UpdateUserAsync(Container container, AppUser user, ILogger log)
        {
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.UserId));
            log.LogInformation($"Update status: {response.StatusCode}");
            return response;
        }

        public static async Task<ItemResponse<AppUser>> CreateUserAsync(Container container, dynamic userData)
        {
            string sub = userData.sub;

            var newUser = new AppUser
            {
                UserId = sub,
                Devices = new List<string>()
            };

            return await container.CreateItemAsync(newUser, new PartitionKey(newUser.UserId));
        }

        public static async Task<ItemResponse<AppUser>> CreateUserAsync(Container container, GoogleUserData userData, ILogger log)
        {
            var newUser = new AppUser
            {
                UserId = userData.sub,
                Devices = new List<string>()
            };
            log.LogInformation(newUser.UserId);
            log.LogInformation(newUser.Devices.ToString());

            var response = await container.CreateItemAsync(newUser);
            if (((int)response.StatusCode) >= 200 && ((int)response.StatusCode) < 300)
                log.LogInformation($"User with ID '{newUser.UserId}' registered successfully in Cosmos DB.");

            return response;
        }
    }
}