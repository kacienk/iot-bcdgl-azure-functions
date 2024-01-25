using System;
using Azure.Core;
using Newtonsoft.Json;


namespace Iotbcdg.Model
{
    public class PairDbEntry : PairData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string PairQueueId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
    }
}