using Shared.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Text;
using System.Threading;

class Program
{
    private const int MAX_MESSAGE_AGE_MINUTES = 5; // Maksymalny wiek wiadomości w minutach

    static async Task Main(string[] args)
    {
        Console.Title = "Emergency Alert Handler";
        AppSettings.Initialize();

        string connectionString = AppSettings.GetServiceBusConnectionString();
        string queueName = "emergency-queue";

        var client = new ServiceBusClient(connectionString);

        Console.WriteLine("Starting queue cleanup...");
        await CleanOldMessages(client, queueName);
        Console.WriteLine("Starting message processor...");

        var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

        var serviceClient = ServiceClient.CreateFromConnectionString(AppSettings.GetServiceConnectionString());
        var registryManager = RegistryManager.CreateFromConnectionString(AppSettings.GetServiceConnectionString());

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var body = args.Message.Body.ToString();
                Console.WriteLine($"\n\tProcessing message from {args.Message.EnqueuedTime}:");
                Console.WriteLine($"\tMessage content: {body}");

                var alert = JsonConvert.DeserializeObject<Alert>(body);
                if (alert == null)
                {
                    Console.WriteLine("\tFailed to deserialize message");
                    return;
                }

                // Używamy DeviceId z wiadomości alertu
                if (string.IsNullOrEmpty(alert.DeviceId))
                {
                    Console.WriteLine("\tError: DeviceId is missing in the alert message");
                    return;
                }

                Console.WriteLine($"\tProcessing alert for device {alert.DeviceId}, type: {alert.AlertType}");
                switch (alert.AlertType)
                {
                    case "Error":
                        await HandleErrorAlert(alert.DeviceId, serviceClient, registryManager);
                        break;
                    case "Production":
                        await HandleProductionAlert(alert.DeviceId, registryManager);
                        break;
                    default:
                        Console.WriteLine($"\tUnknown alert type: {alert.AlertType}");
                        break;
                }

                await args.CompleteMessageAsync(args.Message);
                Console.WriteLine("\tMessage processing completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\tError processing message: {ex.Message}");
                Console.WriteLine($"\tStack trace: {ex.StackTrace}");
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            Console.WriteLine($"Error in processor: {args.Exception}");
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync();
        Console.WriteLine($"Started listening on queue: {queueName}");

        Console.WriteLine("Press any key to end the processing");
        Console.ReadKey();

        await processor.StopProcessingAsync();
    }

    private static async Task CleanOldMessages(ServiceBusClient client, string queueName)
    {
        try
        {
            Console.WriteLine($"Cleaning old messages from queue: {queueName}");
            var receiver = client.CreateReceiver(queueName);
            int removedCount = 0;
            int keptCount = 0;

            // Dodajemy timeout dla operacji czyszczenia
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Pobierz wiadomości z kolejki z timeoutem
                    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 100, TimeSpan.FromSeconds(5));
                    if (messages.Count == 0)
                    {
                        Console.WriteLine("No more messages to process");
                        break;
                    }

                    foreach (var message in messages)
                    {
                        if (IsMessageTooOld(message))
                        {
                            await receiver.CompleteMessageAsync(message);
                            removedCount++;
                            Console.WriteLine($"Removed old message from {message.EnqueuedTime}");
                        }
                        else
                        {
                            // Zwróć wiadomość do kolejki
                            await receiver.AbandonMessageAsync(message);
                            keptCount++;
                            Console.WriteLine($"Kept message from {message.EnqueuedTime} - will be processed normally");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Queue cleanup timed out after 30 seconds");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during message processing: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"Queue cleaning completed. Removed {removedCount} old messages, kept {keptCount} recent messages.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during queue cleanup: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static bool IsMessageTooOld(ServiceBusReceivedMessage message)
    {
        var messageAge = DateTime.UtcNow - message.EnqueuedTime;
        return messageAge.TotalMinutes > MAX_MESSAGE_AGE_MINUTES;
    }

    private static async Task HandleErrorAlert(string deviceId, ServiceClient serviceClient, RegistryManager registryManager)
    {
        Console.WriteLine($"\n\tALERT: Device {deviceId} had errors");

        try
        {
            // Sprawdź czy urządzenie jest połączone
            var device = await registryManager.GetDeviceAsync(deviceId);
            if (device == null)
            {
                Console.WriteLine($"\tError: Device {deviceId} does not exist in IoT Hub");
                return;
            }

            Console.WriteLine($"\tDevice status: {device.Status}, Connection: {device.ConnectionState}");

            if (device.ConnectionState != DeviceConnectionState.Connected)
            {
                Console.WriteLine($"\tWarning: Device {deviceId} is not connected to IoT Hub");
                return;
            }

            // Sprawdź aktualny stan urządzenia
            var twin = await registryManager.GetTwinAsync(deviceId);
            if (twin == null)
            {
                Console.WriteLine($"\tError: Could not get twin for device {deviceId}");
                return;
            }

            // Sprawdź czy urządzenie już jest w stanie Emergency Stop
            if (twin.Properties?.Reported != null && twin.Properties.Reported.Contains("ErrorStatus"))
            {
                string errorStatus = twin.Properties.Reported["ErrorStatus"];
                if (errorStatus.Contains("Emergency Stop"))
                {
                    Console.WriteLine($"\tDevice {deviceId} is already in Emergency Stop state. Ignoring alert.");
                    return;
                }
            }

            // Wywołaj Emergency Stop na urządzeniu z mechanizmem ponownych prób
            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(2);
            bool emergencyStopTriggered = false;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var methodInvocation = new CloudToDeviceMethod("EmergencyStop")
                    {
                        ResponseTimeout = TimeSpan.FromSeconds(30)
                    };

                    Console.WriteLine($"\tAttempt {i + 1}/{maxRetries}: Invoking EmergencyStop method...");
                    var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);

                    Console.WriteLine($"\tEmergency Stop triggered successfully");
                    Console.WriteLine($"\tDevice response: {response.GetPayloadAsJson()}");
                    emergencyStopTriggered = true;
                    break;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    var delay = baseDelay * Math.Pow(2, i);
                    Console.WriteLine($"\tRetry {i + 1}/{maxRetries} after {delay.TotalSeconds}s. Error: {ex.Message}");
                    await Task.Delay(delay);
                }
            }

            if (!emergencyStopTriggered)
            {
                Console.WriteLine($"\tError: Failed to trigger Emergency Stop after {maxRetries} attempts");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\tError triggering Emergency Stop: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\tInner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task HandleProductionAlert(string deviceId, RegistryManager registryManager)
    {
        Console.WriteLine($"PRODUCTION ALERT: Device {deviceId} has low production rate");

        try
        {
            // Sprawdź czy urządzenie istnieje
            var device = await registryManager.GetDeviceAsync(deviceId);
            if (device == null)
            {
                Console.WriteLine($"Error: Device {deviceId} does not exist in IoT Hub");
                return;
            }

            Console.WriteLine($"Device status: {device.Status}, Connection: {device.ConnectionState}");

            // Pobierz aktualny Desired Production Rate z mechanizmem ponownych prób
            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(2);
            Twin twin = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    twin = await registryManager.GetTwinAsync(deviceId);
                    if (twin != null) break;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    var delay = baseDelay * Math.Pow(2, i);
                    Console.WriteLine($"Retry {i + 1}/{maxRetries} getting twin after {delay.TotalSeconds}s. Error: {ex.Message}");
                    await Task.Delay(delay);
                }
            }

            if (twin == null)
            {
                Console.WriteLine($"Error: Could not get twin for device {deviceId} after {maxRetries} attempts");
                return;
            }

            int currentRate = 100; // Domyślna wartość
            if (twin.Properties?.Desired != null && twin.Properties.Desired.Contains("ProductionRate"))
            {
                var rateValue = twin.Properties.Desired["ProductionRate"];
                if (rateValue != null)
                {
                    currentRate = Convert.ToInt32(rateValue);
                }
            }

            // Zmniejsz o 10 punktów
            int newRate = Math.Max(0, currentRate - 10);

            // Aktualizuj Desired Production Rate z mechanizmem ponownych prób
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        ProductionRate = newRate
                    }
                }
            };

            Twin updatedTwin = null;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    updatedTwin = await registryManager.UpdateTwinAsync(deviceId, JsonConvert.SerializeObject(patch), twin.ETag);
                    if (updatedTwin != null) break;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    var delay = baseDelay * Math.Pow(2, i);
                    Console.WriteLine($"Retry {i + 1}/{maxRetries} updating twin after {delay.TotalSeconds}s. Error: {ex.Message}");
                    await Task.Delay(delay);
                }
            }

            if (updatedTwin == null)
            {
                Console.WriteLine($"Error: Could not update twin for device {deviceId} after {maxRetries} attempts");
                return;
            }

            Console.WriteLine($"Production Rate changed: {currentRate}% -> {newRate}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Production Rate: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
}

public class Alert
{
    public string DeviceId { get; set; }
    public DateTime WindowEnd { get; set; }
    public string AlertType { get; set; }
    public int? ErrorCount { get; set; }
    public double? GoodProductionPercentage { get; set; }
}