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

        public static List<DeviceData> ParseDataDbEntreis(List<DataDbEntry> dbEntries)
        {
            List<DeviceData> deviceData = new();

            foreach (var record in dbEntries)
            {
                byte[] decodedBytes = Convert.FromBase64String(record.Body);
                string decodedString = Encoding.UTF8.GetString(decodedBytes);
                DeviceDataBody deviceDataBody = JsonConvert.DeserializeObject<DeviceDataBody>(decodedString);
                DeviceData newDeviceData = new()
                {
                    DeviceId = deviceDataBody.DeviceId,
                    Value = deviceDataBody.Value,
                    Timestamp = record._ts
                };
                deviceData.Add(newDeviceData);
            }

            return deviceData;
        }
    }
}