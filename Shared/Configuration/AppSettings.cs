using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Shared.Configuration
{
    public class AppSettings
    {
        private static IConfiguration _configuration;
        private static DevicesConfiguration _devicesConfiguration;

        public static void Initialize()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
            LoadDevicesConfiguration();
        }

        private static void LoadDevicesConfiguration()
        {
            _devicesConfiguration = new DevicesConfiguration
            {
                Devices = new List<DeviceConfiguration>()
            };

            var devicesSection = _configuration.GetSection("Devices");
            var defaultDevice = devicesSection["DefaultDevice"];

            foreach (var deviceSection in devicesSection.GetChildren().Where(c => c.Key != "DefaultDevice"))
            {
                var device = new DeviceConfiguration
                {
                    Name = deviceSection["Name"],
                    OpcUaName = deviceSection["OpcUaName"],
                    OpcUaServerUrl = deviceSection["OpcUaServerUrl"],
                    IoTHubDeviceId = deviceSection["IoTHubDeviceId"],
                    IoTHubConnectionString = deviceSection["IoTHubConnectionString"],
                    OpcUaNodeIds = new Dictionary<string, string>()
                };

                var nodeIdsSection = deviceSection.GetSection("OpcUaNodeIds");
                foreach (var nodeId in nodeIdsSection.GetChildren())
                {
                    device.OpcUaNodeIds[nodeId.Key] = nodeId.Value.Replace("{DeviceName}", device.OpcUaName);
                }

                _devicesConfiguration.Devices.Add(device);
            }

            _devicesConfiguration.DefaultDevice = defaultDevice;
        }

        public static DeviceConfiguration GetDeviceConfiguration(string deviceName = null)
        {
            deviceName ??= _devicesConfiguration.DefaultDevice;
            return _devicesConfiguration.Devices.FirstOrDefault(d => d.Name == deviceName);
        }

        public static List<DeviceConfiguration> GetAllDevices()
        {
            return _devicesConfiguration.Devices;
        }

        public static string GetServiceConnectionString()
        {
            return _configuration["ServiceController:ConnectionString"];
        }

        public static string GetServiceBusConnectionString()
        {
            return _configuration["ServiceBus:ConnectionString"];
        }

        public static string GetServiceBusQueueName()
        {
            return _configuration["ServiceBus:QueueName"];
        }

        public static IEnumerable<string> GetDeviceNames()
        {
            return _devicesConfiguration.Devices.Select(d => d.Name);
        }

        public static DeviceConfiguration GetDeviceConfig(string deviceName)
        {
            return _devicesConfiguration.Devices.FirstOrDefault(d => d.Name == deviceName);
        }

        public static string GetIoTHubDeviceId(string localName)
        {
            var device = _devicesConfiguration.Devices.FirstOrDefault(d => d.Name == localName);
            return device?.IoTHubDeviceId;
        }

        public static string GetLocalNameFrom(string iotHubDeviceId)
        {
            var device = _devicesConfiguration.Devices.FirstOrDefault(d => d.IoTHubDeviceId == iotHubDeviceId);
            return device?.Name;
        }
    }
}
