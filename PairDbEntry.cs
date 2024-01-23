using System;
using Azure.Core;


namespace Iotbcdg.Model
{
    public class PairDbEntry : PairData
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
    }
}