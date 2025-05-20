using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    internal class FeatureSelector
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
                        System.Console.WriteLine("\n Type your message (confirm with enter):");
                        string messageText = System.Console.ReadLine() ?? string.Empty;

                        System.Console.WriteLine("\n Type your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;


                        await manager.SendMessage(messageText, deviceId);
                        System.Console.WriteLine("Message sent!");
                    }
                    break;
                case 2:
                    {

                        System.Console.WriteLine("\n Type your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;
                        
                        try
                        {
                            var result = await manager.ExecuteDeviveMethod("SendMessages", deviceId);
                            System.Console.WriteLine($"Method executed with status {result}");
                        }catch(DeviceNotFoundException)
                        {
                            System.Console.WriteLine("Device not found!");
                        }

                    }
                    break;
                case 3:
                    {

                        System.Console.WriteLine($"\n Type your property name (confirm with enter):");
                        string propertyName = System.Console.ReadLine() ?? string.Empty;
                        
                        System.Console.WriteLine("\n Type your device ID (confirm with enter):");
                        string deviceId = System.Console.ReadLine() ?? string.Empty;

                        var Random = new Random();
                        await manager.UpdateDesiredTwin(deviceId, propertyName, Random.Next());
                    }
                    break;



                default:
                    break;

            }

        }


        internal static int ReadInput()
        {

            var keyPressed = System.Console.ReadKey();
            var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int result);
            return isParsed ? result : -1;
        }



    }
}
