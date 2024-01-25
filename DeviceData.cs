using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Iotbcdg.Model
{
    public class DeviceData
    {
        public string Id { get; set; }
        public int _ts { get; set; }
        public double Value { get; set; }

        public static async Task<List<DeviceData>> GetDeviceDataAsync(Container container, string deviceId)
        {
            var query = new QueryDefinition($"SELECT * FROM c WHERE c.deviceId = @deviceId")
                .WithParameter("@deviceId", deviceId);

            List<DeviceData> deviceData = new();

            using var iterator = container.GetItemQueryIterator<DeviceData>(query);
            while (iterator.HasMoreResults)
            {
                foreach (var record in await iterator.ReadNextAsync())
                {
                    deviceData.Add(record);
                }
            }

            return deviceData;
        }
    }
}