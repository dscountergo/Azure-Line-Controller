
using Device;
using Microsoft.Azure.Devices.Client;


string deviceConnectionString =  "HostName=UL-Hub.azure-devices.net;DeviceId=test_device;SharedAccessKey=aeFLPThI3414+tx6ygfRlc0ucpcJFcAP/raFGdbXxws=";
using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
await deviceClient.OpenAsync();

var device = new VirtualDevice(deviceClient);

await device.InitializeHandlers();
await device.UpdateTwinAsync();

Console.WriteLine("Connection Success!");



await device.SendMessages(10, 1000);
Console.WriteLine("Finished! Press Enter to close..");
Console.ReadLine();