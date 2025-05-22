using System;
using Shared.Configuration; // Zakładam, że AppSettings.cs znajduje się w tym namespace


// DODAĆ AUTOMATYCZNE GENEROWANIE PLIKU appsettings.json W PRZYPADKU JEGO BRAKU

namespace ConfigTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppSettings.Initialize();

            Console.WriteLine("Azure IoT Hub Connection String: " + AppSettings.GetAzureIoTHubConnectionString());
            Console.WriteLine("Device ID: " + AppSettings.GetDeviceId());

            Console.WriteLine("OPC UA Server URL: " + AppSettings.GetOpcUaServerUrl());
            Console.WriteLine("OPC UA Device Name: " + AppSettings.GetOpcUaDeviceName());
            Console.WriteLine("NodeId for Temperature: " + AppSettings.GetOpcUaNodeId("Temperature"));

            Console.WriteLine("Service Bus Connection String: " + AppSettings.GetServiceBusConnectionString());
            Console.WriteLine("Service Bus Queue Name: " + AppSettings.GetServiceBusQueueName());
        }
    }
}
