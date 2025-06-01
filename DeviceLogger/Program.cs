using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Shared;
using Shared.Configuration;

namespace DeviceLogger
{
    public static class Program
    {
        private static readonly ConcurrentDictionary<string, BlockingCollection<string>> deviceLogs = new();
        private static readonly object consoleLock = new();
        private static RabbitMQService? _rabbitMQService;
        private static readonly Dictionary<string, ConsoleColor> LogColorKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            {"błąd", ConsoleColor.Red},
            {"error", ConsoleColor.Red}
            // Możesz dodać kolejne słowa-klucze i kolory tutaj
        };

        public static async Task Main(string[] args)
        {
            Console.Title = "Device Logger";
            AppSettings.Initialize();

            try
            {
                _rabbitMQService = new RabbitMQService();
                _rabbitMQService.SubscribeToLogs(OnLogReceived);

            Console.WriteLine("Device Logger started. Press any key to exit...");
            Console.ReadKey();
        }
            catch (Exception ex)
                    {
                Console.WriteLine($"Error starting Device Logger: {ex.Message}");
                    }
            finally
            {
                _rabbitMQService?.Dispose();
            }
        }

        private static void OnLogReceived(string deviceId, string message)
        {
            if (!deviceLogs.ContainsKey(deviceId))
            {
                deviceLogs[deviceId] = new BlockingCollection<string>();
            }

            deviceLogs[deviceId].Add($"[{DateTime.Now:HH:mm:ss}] {message}");

            lock (consoleLock)
            {
                // Nazwa urządzenia zawsze na żółto
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"[{deviceId}]");
                Console.ResetColor();
                Console.Write(" ");

                // Dobierz kolor na podstawie słownika
                var color = ConsoleColor.Blue; // domyślny
                foreach (var kvp in LogColorKeywords)
                {
                    if (message.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        color = kvp.Value;
                        break;
                    }
                }
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private static ConsoleColor GetDeviceColor(string deviceId)
        {
            // Generuj kolor na podstawie nazwy urządzenia
            int hash = deviceId.GetHashCode();
            ConsoleColor[] colors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
            return colors[Math.Abs(hash) % colors.Length];
        }
    }
} 