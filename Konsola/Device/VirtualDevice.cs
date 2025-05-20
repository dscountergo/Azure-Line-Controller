using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Text;

namespace Device
{
    public class VirtualDevice
    {
        private readonly DeviceClient client;

        public VirtualDevice(DeviceClient deviceClient)
            
        {
            this.client = deviceClient;
                
        }


        #region Sending Messages D2C

        public async Task SendMessages(int nrOfMessages, int delay)
        {

            var rnd = new Random();
            Console.WriteLine($"Device sending {nrOfMessages} messages to Iot Hub.. \n");

            for (int count = 0; count < nrOfMessages; count++)
            {
                var data = new
                {
                    temperature = rnd.Next(20, 35),
                    humidity = rnd.Next(60, 80),
                    msgCount = count
                };

                var dataString = JsonConvert.SerializeObject(data);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                eventMessage.ContentType = "application/json";
                eventMessage.ContentEncoding = "utf-8";
                eventMessage.Properties.Add("temperatureAlert", (data.temperature > 30 ? "true" : "false"));


                Console.WriteLine($"\t {DateTime.Now.ToLocalTime()} > Sending message {count}, Data: [{dataString}]");


                await client.SendEventAsync(eventMessage);

                if (count < nrOfMessages - 1)
                {
                    await Task.Delay(delay);
                }

                Console.WriteLine();
            }

        }
        #endregion

        #region Receiving Messages C2D

        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"\t {DateTime.Now}> C2D message callback - message received with Id = {receivedMessage.MessageId}");
            PrintMessage(receivedMessage);

            await client.CompleteAsync(receivedMessage);
            Console.WriteLine($"\t {DateTime.Now}> Completed C2D message received with Id = {receivedMessage.MessageId}");

        }


        private void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\t Property [{propCount++}] > Key={prop.Key} Value={prop.Value}");
            }
        }
        #endregion


        #region direct methods


        private async Task<MethodResponse> SendMessageHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { nrofMessages = default(int), delay = default(int) });
            await SendMessages(payload.nrofMessages, payload.delay);
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");
            await Task.Delay(1000);
            return new MethodResponse(0);
        }
        #endregion

        #region Device Twin



        public async Task UpdateTwinAsync()
        {
            var twin = await client.GetTwinAsync();
            Console.WriteLine($"\n Initial twin value received: \n {JsonConvert.SerializeObject(twin,Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
            await client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {

            Console.WriteLine($"\n Desired property change: \n\t {JsonConvert.SerializeObject(desiredProperties)}");
            Console.WriteLine("\t Sending current time as reported property");

            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertReceived"] = DateTime.Now;
            await client.UpdateReportedPropertiesAsync(reportedProperties);

        }

        #endregion

        public async Task InitializeHandlers()
        {
            await client.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, client);

            await client.SetMethodHandlerAsync("SendMessages", SendMessageHandler, client);
            await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);

            await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client);

        }
    }
}
