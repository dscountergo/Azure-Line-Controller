using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSdkDemo.Console
{
    internal static class FeatureSelector
    {
        public static void PrintMenu()
        {
            System.Console.WriteLine(@"
1 - C2D
2 - Direct Method
3 - Device Twin
4 - Device Status Control
0 - Exit");
        }

        public static async Task Execute(int feature, Lib.IoTHubManager manager)
        {
            switch (feature) 
            {
                case 1:
                    {
                        System.Console.WriteLine("\nType your message (confirm with Enter):");
                        string messageText = System.Console.ReadLine() ?? string.Empty;

                        System.Console.WriteLine("\nType your device Id (confirm with Enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        await manager.SendMessage(messageText, deviceId);
                    }
                    break;
                case 2:
                    {
                        System.Console.WriteLine("\nType your device Id (confirm with Enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;
                        try
                        {
                            System.Console.WriteLine("\nAvailable methods:");
                            System.Console.WriteLine("1 - SendMessages");
                            System.Console.WriteLine("2 - EmergencyStop");
                            System.Console.WriteLine("3 - ClearErrors");
                            System.Console.WriteLine("\nSelect method number:");
                            
                            string methodNumber = System.Console.ReadLine() ?? string.Empty;
                            string methodName = methodNumber switch
                            {
                                "1" => "SendMessages",
                                "2" => "EmergencyStop",
                                "3" => "ClearErrors",
                                _ => "SendMessages"
                            };

                            var result = await manager.ExecuteDeviceMethod(methodName, deviceId);
                            System.Console.WriteLine($"Method {methodName} executed with status {result}");
                        }
                        catch(DeviceNotFoundException)
                        {
                            System.Console.WriteLine("Device not connected!");
                        }
                    }
                    break;
                case 3:
                    {
                        System.Console.WriteLine("\nType your device Id (confirm with Enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        System.Console.WriteLine("\nType property name (confirm with Enter):");
                        string propertyName = System.Console.ReadLine() ?? string.Empty;

                        var random = new Random();
                        await manager.UpdateDesiredTwin(deviceId, propertyName,random.Next());
                    }
                    break;
                case 4:
                    {
                        System.Console.WriteLine("\nType your device Id (confirm with Enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;
                        try
                        {
                            System.Console.WriteLine("\nDevice Status Control:");
                            System.Console.WriteLine("1 - Start Device");
                            System.Console.WriteLine("2 - Stop Device");
                            System.Console.WriteLine("\nSelect option:");
                            
                            string option = System.Console.ReadLine() ?? string.Empty;
                            bool isRunning = option == "1";
                            
                            var result = await manager.SetDeviceStatus(deviceId, isRunning);
                            System.Console.WriteLine($"Device {(isRunning ? "started" : "stopped")} with status {result}");
                        }
                        catch(DeviceNotFoundException)
                        {
                            System.Console.WriteLine("Device not connected!");
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        internal static int ReadInput()
        {
            var keyPressed = System.Console.ReadKey();
            var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int value);
            return isParsed ? value : -1;
        }

    }
}
