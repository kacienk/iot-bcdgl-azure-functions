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
        public string Id { get; set; }
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

        public static async Task UpdateUserAsync(Container container, AppUser user)
        {
            await container.UpsertItemAsync(user, new PartitionKey(user.Id));
        }

        public static async Task UpdateUserAsync(Container container, AppUser user, ILogger log)
        {
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.Id));
            log.LogInformation($"Update status: {response.StatusCode}");
        }

        public static async Task<ItemResponse<AppUser>> CreateUserAsync(Container container, dynamic userData)
        {
            string sub = userData.sub;

            var newUser = new AppUser
            {
                Id = sub,
                Devices = new List<string>()
            };

            return await container.CreateItemAsync(newUser, new PartitionKey(newUser.Id));
        }

        public static async Task<ItemResponse<AppUser>> CreateUserAsync(Container container, dynamic userData, ILogger log)
        {
            string sub = userData.sub;

            var newUser = new AppUser
            {
                Id = sub,
                Devices = new List<string>()
            };

            var response = await container.CreateItemAsync(newUser, new PartitionKey(newUser.Id));
            if (((int)response.StatusCode) >= 200 && ((int)response.StatusCode) < 300)
                log.LogInformation($"User with ID '{newUser.Id}' registered successfully in Cosmos DB.");

            return response;
        }
    }
}