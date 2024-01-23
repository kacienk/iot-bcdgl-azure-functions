using System;
using Azure.Core;
using Newtonsoft.Json;


namespace Iotbcdg.Model
{
    public class PairDbEntry : PairData
    {
        public string PairQueueId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
    }
}