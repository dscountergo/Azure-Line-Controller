using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Text;

namespace ServiceLib
{
    public class IoTHubManager
    {
        private readonly ServiceClient client;
        private readonly RegistryManager registry;
        public IoTHubManager(ServiceClient client, RegistryManager registry)
        {
            this.client = client;
            this.registry = registry;
        }

        // C2D
        public async Task SendMessage(string textMessage, string deviceId)
        {
            var messageBody = new { text = textMessage };
            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)));
            message.MessageId = Guid.NewGuid().ToString();
            await client.SendAsync(deviceId, message);
        }
        // Direct Method
        public async Task<int> ExecuteDeviceMethod(string methodName, string deviceId)
        {
            var method = new CloudToDeviceMethod(methodName);

            // Dla SendMessages używamy domyślnych parametrów
            if (methodName == "SendMessages")
            {
                var methodBody = new { nrOfMessages = 5, delay = 500 };
                method.SetPayloadJson(JsonConvert.SerializeObject(methodBody));
            }
            // Dla EmergencyStop i ClearErrors nie potrzebujemy parametrów
            else
            {
                method.SetPayloadJson("{}");
            }

            var result = await client.InvokeDeviceMethodAsync(deviceId, method);
            return result.Status;
        }
        // Device Twin
        public async Task UpdateDesiredTwin(string deviceId, string propertyName, dynamic propertyValue)
        {
            var twin = await registry.GetTwinAsync(deviceId);
            twin.Properties.Desired[propertyName] = propertyValue;
            await registry.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
        }

        public async Task<int> ClearDeviceErrors(string deviceId)
        {
            var method = new CloudToDeviceMethod("ClearErrors");
            var result = await client.InvokeDeviceMethodAsync(deviceId, method);
            return result.Status;
        }

        public async Task<int> SetDeviceStatus(string deviceId, bool isRunning)
        {
            var method = new CloudToDeviceMethod("SetDeviceStatus");
            var methodBody = new { isRunning = isRunning };
            method.SetPayloadJson(JsonConvert.SerializeObject(methodBody));
            var result = await client.InvokeDeviceMethodAsync(deviceId, method);
            return result.Status;
        }

    }
}