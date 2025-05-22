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
                            var result = await manager.ExecuteDeviceMethod("SendMessages", deviceId);
                            System.Console.WriteLine($"Method executed with static {result}");
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
