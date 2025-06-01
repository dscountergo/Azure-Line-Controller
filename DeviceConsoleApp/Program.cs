using DeviceLogic;
using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Threading;
using Shared.Configuration;

AppSettings.Initialize();

var deviceConfig = AppSettings.GetDeviceConfiguration();
string deviceConnectionString = deviceConfig.IoTHubConnectionString;

using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
await deviceClient.OpenAsync();
var device = new VirtualDevice(deviceConfig);
Console.WriteLine("Connection success");
await device.InitializeHandlers();
await device.UpdateTwinAsync();

var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

while (await periodicTimer.WaitForNextTickAsync())
{
    await device.TimerSendingMessages();
}

Console.WriteLine("Finished! Press key to close...");
Console.ReadLine();