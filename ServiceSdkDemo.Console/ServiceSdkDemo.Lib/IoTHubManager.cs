﻿using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Text;

namespace ServiceSdkDemo.Lib
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
            await client.SendAsync(deviceId,message);
        }
        // Direct Method
        public async Task<int> ExecuteDeviceMethod(string methodName, string deviceId)
        {
            var method = new CloudToDeviceMethod(methodName);
            var methodBody = new { nrOfMessages = 5, delay = 500 };
            method.SetPayloadJson(JsonConvert.SerializeObject(methodBody));

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
        
    }
}