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

        public static void Initialize()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public static string GetAzureIoTHubConnectionString()
        {
            return _configuration["AzureIoTHub:ConnectionString"];
        }

        public static string GetDeviceId()
        {
            return _configuration["AzureIoTHub:DeviceId"];
        }

        public static string GetServiceConnectionString()
        {
            return _configuration["ServiceController:ConnectionString"];
        }


        public static string GetOpcUaServerUrl()
        {
            return _configuration["OPCUA:ServerUrl"];
        }

        public static string GetOpcUaDeviceName()
        {
            return _configuration["OPCUA:DeviceName"];
        }

        public static string GetOpcUaNodeId(string nodeName)
        {
            var nodeId = _configuration[$"OPCUA:NodeIds:{nodeName}"];
            return nodeId.Replace("{DeviceName}", GetOpcUaDeviceName());
        }

        public static string GetServiceBusConnectionString()
        {
            return _configuration["ServiceBus:ConnectionString"];
        }

        public static string GetServiceBusQueueName()
        {
            return _configuration["ServiceBus:QueueName"];
        }
    }
}
