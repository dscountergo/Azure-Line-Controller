
using Lib;
using Microsoft.Azure.Devices;


string serviceConnectionString = "HostName=UL-Hub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=jybcyeJZG0o98gH4P5c6Gtu25gr/llJwAAIoTGe7gCc=";




using var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString);
using var registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);



var manager = new IoTHubManager(serviceClient, registryManager);


int input;

do
{
    FeatureSelector.PrintMenu();
    input = FeatureSelector.ReadInput();
    await FeatureSelector.Execute(input, manager);
} while (input != 0);


Console.ReadLine();
