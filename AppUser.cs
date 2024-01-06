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
        public string Email { get; set; }
        public List<string> Devices { get; set; } = new List<string>();
        public string Password { get; set; }
        public string Salt { get; set; }

        public static async Task<AppUser> GetUserByIdAsync(Container container, string userId)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.id = @userId")
                .WithParameter("@userId", userId);

            var iterator = container.GetItemQueryIterator<AppUser>(query);
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        public static async Task<AppUser> GetUserByEmailAsync(Container container, string email)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.id = @email")
                .WithParameter("@email", email);

            var iterator = container.GetItemQueryIterator<AppUser>(query);
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        public static async Task UpdateUserAsync(Container container, AppUser user)
        {
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.Id));
        }

        public static async Task UpdateUserAsync(Container container, AppUser user, ILogger log)
        {
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.Id));
            log.LogInformation($"Update status: {response.StatusCode}");
        }

        public static async Task CreateUserAsync(Container container, dynamic userData, string pepper)
        {
            AuthHandler authHandler = new(pepper);
            string email = userData.email;
            string password = userData.password;
            (string hashPassword, string salt) = authHandler.HashPassword(password);

            var newUser = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Password = hashPassword,
                Salt = salt,
                Devices = new List<string>()
            };

            await container.CreateItemAsync(newUser, new PartitionKey(newUser.Id));
        }

        public static async Task CreateUserAsync(Container container, dynamic userData, string pepper, ILogger log)
        {
            AuthHandler authHandler = new(pepper);
            string email = userData.email;
            string password = userData.password;
            (string hashPassword, string salt) = authHandler.HashPassword(password);

            var newUser = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Password = hashPassword,
                Salt = salt,
                Devices = new List<string>()
            };

            await container.CreateItemAsync(newUser, new PartitionKey(newUser.Id));
            Console.WriteLine($"User with ID '{newUser.Id}' registered successfully in Cosmos DB.");
        }
    }
}