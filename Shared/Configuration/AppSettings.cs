using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace Shared.Configuration
{
    public class AppSettings
    {
        private static IConfiguration _configuration;
        private static DevicesConfiguration _devicesConfiguration;

        private static string FindSharedDirectory()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var shared = Path.Combine(dir.FullName, "Shared");
                if (Directory.Exists(shared))
                    return shared;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Cannot find 'Shared' directory in parent folders.");
        }

        private static bool ConfigContainsPlaceholders(string configPath)
        {
            var text = File.ReadAllText(configPath);
            return System.Text.RegularExpressions.Regex.IsMatch(text, "<[^>]+>");
        }

        public static void Initialize()
        {
            var sharedDir = FindSharedDirectory();
            var configPath = Path.Combine(sharedDir, "config.json");
            var examplePath = Path.Combine(sharedDir, "config.example.json");
            bool configJustCreated = false;
            // Globalny mutex dla całego systemu aby zapobiec race condition przy tworzeniu pliku konfiguracyjnego
            using (var mutex = new System.Threading.Mutex(false, "Global\\AzureLineControllerConfigInit"))
            {
                if (mutex.WaitOne(10000)) // czekaj maksymalnie 10 sekund na dostęp
                {
                    try
                    {
                        if (!File.Exists(configPath))
                        {
                            if (File.Exists(examplePath))
                            {
                                File.Copy(examplePath, configPath);
                                configJustCreated = true;
                            }
                            else
                            {
                                throw new FileNotFoundException($"Missing configuration and example file: {configPath}, {examplePath}");
                            }
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            if (ConfigContainsPlaceholders(configPath))
            {
                if (configJustCreated)
                {
                    Console.WriteLine($"Configuration file generated: {configPath}. Please fill it with your data and restart the application.");
                }
                else
                {
                    Console.WriteLine($"Configuration file contains placeholder values. Please fill in your real connection strings and device names before running the application.\nFile: {configPath}");
                }
                Console.WriteLine("Application will now exit.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(2);
            }
            var builder = new ConfigurationBuilder()
                .SetBasePath(sharedDir)
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
