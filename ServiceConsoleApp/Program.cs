using System;
using Microsoft.Azure.Devices;
using ServiceLib;
using Shared.Configuration;

namespace ServiceConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.Title = "Service Console";

            AppSettings.Initialize();
            var serviceClient = ServiceClient.CreateFromConnectionString(AppSettings.GetServiceConnectionString());
            var registryManager = RegistryManager.CreateFromConnectionString(AppSettings.GetServiceConnectionString());
            var manager = new IoTHubManager(serviceClient, registryManager);

            while (true)
            {
                FeatureSelector.PrintMainMenu();
                var key = System.Console.ReadKey();

                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    continue;
                }

                if (int.TryParse(key.KeyChar.ToString(), out int option))
                {
                    await FeatureSelector.ExecuteMainMenu(option, manager);
                }
            }
        }
    }
}
