using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceLogic;
using Shared.Configuration;
using System.Collections.Concurrent;

namespace ServiceConsoleApp
{
    internal static class FeatureSelector
    {
        private static bool isConnected = false;
        //private static List<string> connectedDevices = new List<string>();
        private static List<string> allDevices = new List<string>();
        private static ConnectionFilter currentFilter = ConnectionFilter.All;
        private static readonly ConcurrentDictionary<string, Task> deviceTasks = new();
        private static readonly ConcurrentDictionary<string, VirtualDevice> activeDevices = new();

        private enum ConnectionFilter
        {
            All,
            Connected,
            Disconnected
        }

        public static void PrintMainMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine(@"
=== IoT Hub Management Console ===
1 - Devices connection panel
2 - Active devices management
0 - Exit

Press 'ESC' to go back
");
        }

        public static void PrintConnectionMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine(@"
=== Connection status ===
Current filter: " + currentFilter.ToString() + @"

Devices:");

            var filteredDevices = GetFilteredDevices();
            if (filteredDevices.Count == 0)
            {
                System.Console.WriteLine("No devices found");
            }
            else
            {
                for (int i = 0; i < filteredDevices.Count; i++)
                {
                    bool isDeviceConnected = deviceTasks.ContainsKey(filteredDevices[i]);
                    System.Console.WriteLine($"{i + 1} - {filteredDevices[i]} ({(isDeviceConnected ? "Online" : "Offline")})");
                }
            }

            System.Console.WriteLine(@"
Filter options:
A - Show all devices
C - Show connected devices
D - Show disconnected devices
0 - Back to main menu

Press 'ESC' to go back
");
        }

        private static List<string> GetFilteredDevices()
        {
            return currentFilter switch
            {
                ConnectionFilter.Connected => allDevices.Where(d => deviceTasks.ContainsKey(d)).ToList(),
                ConnectionFilter.Disconnected => allDevices.Where(d => !deviceTasks.ContainsKey(d)).ToList(),
                _ => allDevices
            };
        }

        public static void PrintDeviceManagementMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine("\n=== Device Management ===\nConnected Devices:");

            var runningDevices = deviceTasks.Keys.ToList();
            if (runningDevices.Count == 0)
            {
                System.Console.WriteLine("No devices connected");
            }
            else
            {
                for (int i = 0; i < runningDevices.Count; i++)
                {
                    System.Console.WriteLine($"{i + 1} - {runningDevices[i]} (Online)");
                }
            }

            System.Console.WriteLine("\n0 - Back to Main Menu\n\nPress 'ESC' to go back\n");
        }

        public static void PrintDeviceMenu(string deviceId)
        {
            System.Console.Clear();
            System.Console.WriteLine($@"
=== Device: {deviceId} ===
1 - Send C2D Message
2 - Execute Direct Method
3 - Update Device Twin
0 - Back to Device List

Press 'ESC' to go back
");
        }

        public static async Task ExecuteMainMenu(int option, ServiceLib.IoTHubManager manager)
        {
            switch (option)
            {
                case 1:
                    await HandleConnectionMenu(manager);
                    break;
                case 2:
                    await HandleDeviceManagementMenu(manager);
                    break;
                case 0:
                    Environment.Exit(0);
                    break;
            }
        }

        private static async Task HandleConnectionMenu(ServiceLib.IoTHubManager manager)
        {
            // Załaduj listę urządzeń z konfiguracji
            allDevices = AppSettings.GetDeviceNames().ToList();

            while (true)
            {
                PrintConnectionMenu();
                var key = System.Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (char.ToUpper(key.KeyChar) == 'A')
                {
                    currentFilter = ConnectionFilter.All;
                    continue;
                }
                else if (char.ToUpper(key.KeyChar) == 'C')
                {
                    currentFilter = ConnectionFilter.Connected;
                    continue;
                }
                else if (char.ToUpper(key.KeyChar) == 'D')
                {
                    currentFilter = ConnectionFilter.Disconnected;
                    continue;
                }

                if (int.TryParse(key.KeyChar.ToString(), out int option))
                {
                    if (option == 0)
                    {
                        return;
                    }

                    var filteredDevices = GetFilteredDevices();
                    if (option > 0 && option <= filteredDevices.Count)
                    {
                        string selectedDevice = filteredDevices[option - 1];
                        bool isDeviceConnected = deviceTasks.ContainsKey(selectedDevice);

                        if (isDeviceConnected)
                        {
                            await StopDevice(selectedDevice);
                        }
                        else
                        {
                            await StartDevice(selectedDevice);
                        }
                    }
                }
            }
        }

        private static async Task StartDevice(string deviceId)
        {
            if (deviceTasks.ContainsKey(deviceId))
            {
                return;
            }

            var deviceConfig = AppSettings.GetDeviceConfig(deviceId);
            if (deviceConfig == null)
            {
                System.Console.WriteLine($"No configuration found for device {deviceId}");
                return;
            }

            var device = new VirtualDevice(deviceConfig);
            activeDevices[deviceId] = device;

            var task = Task.Run(async () =>
            {
                try
                {
                    await device.StartAsync();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error on device {deviceId}: {ex.Message}");
                    await StopDevice(deviceId);
                }
            });

            deviceTasks[deviceId] = task;
        }

        private static async Task StopDevice(string deviceId)
        {
            if (!deviceTasks.ContainsKey(deviceId))
            {
                return;
            }

            if (activeDevices.TryRemove(deviceId, out var device))
            {
                await device.StopAsync();
            }

            if (deviceTasks.TryRemove(deviceId, out var task))
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error while stopping device {deviceId}: {ex.Message}");
                }
            }
        }

        private static async Task HandleDeviceManagementMenu(ServiceLib.IoTHubManager manager)
        {
            while (true)
            {
                PrintDeviceManagementMenu();
                var key = System.Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (int.TryParse(key.KeyChar.ToString(), out int option))
                {
                    if (option == 0)
                    {
                        return;
                    }

                    var runningDevices = deviceTasks.Keys.ToList();
                    if (option > 0 && option <= runningDevices.Count)
                    {
                        await HandleDeviceMenu(manager, runningDevices[option - 1]);
                    }
                }
            }
        }

        private static async Task HandleDeviceMenu(ServiceLib.IoTHubManager manager, string deviceId)
        {
            while (true)
            {
                PrintDeviceMenu(deviceId);
                var key = System.Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (int.TryParse(key.KeyChar.ToString(), out int option))
                {
                    switch (option)
                    {
                        case 1:
                            await HandleC2DMessage(manager, deviceId);
                            break;
                        case 2:
                            await HandleDirectMethod(manager, deviceId);
                            break;
                        case 3:
                            await HandleDeviceTwin(manager, deviceId);
                            break;
                        case 0:
                            return;
                    }
                }
            }
        }

        private static async Task HandleC2DMessage(ServiceLib.IoTHubManager manager, string deviceId)
        {
            System.Console.Clear();
            System.Console.WriteLine("\nType your message (confirm with Enter) or 'ESC' to cancel:");
            var key = System.Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

            string messageText = key.KeyChar.ToString() + (System.Console.ReadLine() ?? string.Empty);
            string iotHubDeviceId = Shared.Configuration.AppSettings.GetIoTHubDeviceId(deviceId);
            await manager.SendMessage(messageText, iotHubDeviceId);
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        private static async Task HandleDirectMethod(ServiceLib.IoTHubManager manager, string deviceId)
        {
            while (true)
            {
                System.Console.Clear();
                System.Console.WriteLine(@"
=== Direct Methods ===
1 - SendMessages
2 - EmergencyStop
3 - ClearErrors
0 - Back to Device Menu

Press 'ESC' to go back
");

                var key = System.Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (int.TryParse(key.KeyChar.ToString(), out int option))
                {
                    if (option == 0)
                    {
                        return;
                    }

                    string methodName = option switch
                    {
                        1 => "SendMessages",
                        2 => "EmergencyStop",
                        3 => "ClearErrors",
                        _ => null
                    };

                    if (methodName != null)
                    {
                        if (methodName == "EmergencyStop")
                        {
                            System.Console.Clear();
                            System.Console.WriteLine("\nWARNING: This will trigger an emergency stop!");
                            System.Console.WriteLine("Type 'yes' to confirm (or 'ESC' to cancel):");

                            var confirmationKey = System.Console.ReadKey();
                            if (confirmationKey.Key == ConsoleKey.Escape)
                            {
                                System.Console.WriteLine("\nOperation cancelled.");
                                System.Console.WriteLine("\nPress any key to continue...");
                                System.Console.ReadKey();
                                continue;
                            }

                            string confirmation = confirmationKey.KeyChar.ToString() + (System.Console.ReadLine() ?? "");
                            if (confirmation.ToLower() != "yes")
                            {
                                System.Console.WriteLine("\nOperation cancelled.");
                                System.Console.WriteLine("\nPress any key to continue...");
                                System.Console.ReadKey();
                                continue;
                            }
                        }


                        try
                        {
                            string iotHubDeviceId = Shared.Configuration.AppSettings.GetIoTHubDeviceId(deviceId);
                            var result = await manager.ExecuteDeviceMethod(methodName, iotHubDeviceId);
                            System.Console.WriteLine($"\nMethod {methodName} executed with status {result}");
                        }
                        catch (DeviceNotFoundException)
                        {
                            System.Console.WriteLine($"\nCalled method: {methodName} for device: {deviceId}, cloud device id:{Shared.Configuration.AppSettings.GetIoTHubDeviceId(deviceId)}");
                            System.Console.WriteLine("\nDevice not connected!");
                        }
                        System.Console.WriteLine("\nPress any key to continue...");
                        System.Console.ReadKey();
                    }
                }
            }
        }

        private static async Task HandleDeviceTwin(ServiceLib.IoTHubManager manager, string deviceId)
        {
            System.Console.Clear();
            System.Console.WriteLine("\nType property name (confirm with Enter) or 'ESC' to cancel:");
            var key = System.Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

            string propertyName = key.KeyChar.ToString() + (System.Console.ReadLine() ?? string.Empty);
            var random = new Random();
            string iotHubDeviceId = Shared.Configuration.AppSettings.GetIoTHubDeviceId(deviceId);
            await manager.UpdateDesiredTwin(iotHubDeviceId, propertyName, random.Next());
            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }

        internal static int ReadInput()
        {
            var keyPressed = System.Console.ReadKey();
            var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int value);
            return isParsed ? value : -1;
        }
    }
}
