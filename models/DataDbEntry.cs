using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Iotbcdg.Model
{
  public class DataDbEntry
  {
    public string Body { get; set; }
    public int _ts { get; set; }

    public static async Task<List<DataDbEntry>> GetDataDbEntryAsync(Container container, string deviceId)
    {
      var query = new QueryDefinition($"SELECT * FROM c WHERE c.deviceId = @deviceId")
          .WithParameter("@deviceId", deviceId);

      List<DataDbEntry> deviceData = new();

      using var iterator = container.GetItemQueryIterator<DataDbEntry>(query);
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