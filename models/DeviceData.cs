using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Iotbcdg.Model
{
    public class DeviceData
    {
        public string DeviceId { get; set; }
        public int Timestamp { get; set; }
        public double Value { get; set; }

        public static async Task<List<DeviceData>> GetDeviceDataAsync(Container container, string deviceId)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.deviceId = @deviceId")
                .WithParameter("@deviceId", deviceId);

            List<DeviceData> deviceData = new();

            using var iterator = container.GetItemQueryIterator<string>(query);
            while (iterator.HasMoreResults)
            {
                foreach (var record in await iterator.ReadNextAsync())
                {
                    byte[] decodedBytes = Convert.FromBase64String(record);
                    string decodedString = Encoding.UTF8.GetString(decodedBytes);
                    DeviceData data = JsonConvert.DeserializeObject<DeviceData>(decodedString);

                    deviceData.Add(data);
                }
            }

            return deviceData;
        }
    }
}