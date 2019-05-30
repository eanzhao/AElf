using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network
{
    public class RequestMetric
    {
        public long RoundTripTime { get; set; }
        public string MethodName { get; set; }
        public string Info { get; set; }
        public Timestamp RequestTime { get; set; }
    }
}