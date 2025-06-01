using System;
using System.Collections.Generic;

namespace Shared.Configuration
{
    public class DeviceConfiguration
    {
        public string Name { get; set; }
        public string OpcUaName { get; set; }
        public string OpcUaServerUrl { get; set; }
        public string IoTHubDeviceId { get; set; }
        public string IoTHubConnectionString { get; set; }
        public Dictionary<string, string> OpcUaNodeIds { get; set; }
    }

    public class DevicesConfiguration
    {
        public List<DeviceConfiguration> Devices { get; set; }
        public string DefaultDevice { get; set; }
    }
}