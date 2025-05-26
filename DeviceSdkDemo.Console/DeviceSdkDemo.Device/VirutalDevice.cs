using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.ComponentModel.Design;
using System.Net.Mime;
using System.Text;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
using Shared.Configuration;

namespace DeviceSdkDemo.Device
{
    public class VirutalDevice
    {
        private readonly DeviceClient deviceClient;
        private int lastErrorState = 0;
        private readonly string OPCstring;
        private readonly string DeviceName;
        private int lastDesiredProductionRate = -1;
        private readonly string iotHubDeviceId;


        public VirutalDevice(DeviceClient DeviceClient)
        {
            deviceClient = DeviceClient;
            OPCstring = AppSettings.GetOpcUaServerUrl();
            DeviceName = AppSettings.GetOpcUaDeviceName();
            iotHubDeviceId = AppSettings.GetDeviceId();
        }

        public async Task TimerSendingMessages()
        {
            var client = new OpcClient(OPCstring);
            client.Connect();

            var ProductionStatus = new OpcReadNode($"ns=2;s={DeviceName}/ProductionStatus");
            int RetValues = client.ReadNode(ProductionStatus).As<int>();
            
            var ProductionRate = new OpcReadNode($"ns=2;s={DeviceName}/ProductionRate");
            int currentRate = client.ReadNode(ProductionRate).As<int>();
            
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceName}/DeviceError");
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();

            client.Disconnect();

            // Sprawdzamy czy aktualna wartość Production Rate różni się od Desired
            var twin = await deviceClient.GetTwinAsync();
            if (twin.Properties.Desired.Contains("ProductionRate"))
            {
                int desiredRate = twin.Properties.Desired["ProductionRate"];
                if (currentRate != desiredRate)
                {
                    // Jeśli wartość się różni, próbujemy przywrócić żądaną wartość
                    await SetProductionRate(desiredRate);
                }
            }
            
            if (RetValues == 1)
            {
                await SendMessages(1, 1);
            }
            else
            {
                Console.WriteLine("Device Offline");
            }
        }

        #region Sending Messages
        public async Task SendMessages(int nrOfMessages, int delay)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();


            var data = new
            {
                DeviceId = iotHubDeviceId,
                ProductionStatus = client.ReadNode($"ns=2;s={DeviceName}/ProductionStatus").Value,
                WorkorderId = client.ReadNode($"ns=2;s={DeviceName}/WorkorderId").Value,
                Temperature = client.ReadNode($"ns=2;s={DeviceName}/Temperature").Value,
                GoodCount = client.ReadNode($"ns=2;s={DeviceName}/GoodCount").Value,
                BadCount = client.ReadNode($"ns=2;s={DeviceName}/BadCount").Value,

            };


            var ProductionRate = new OpcReadNode($"ns=2;s={DeviceName}/ProductionRate");
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceName}/DeviceError");
            int ProductionRateNode = client.ReadNode(ProductionRate).As<int>();
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();

            await UpdateTwinData(ProductionRateNode, DeviceErrorNode);

            var dataString = JsonConvert.SerializeObject(data);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";
            Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Data: [{dataString}]");

            await deviceClient.SendEventAsync(eventMessage);
            client.Disconnect();
        }
        #endregion
        #region Receive Message
        private async Task OnC2MessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"\t{DateTime.Now} > C2D message callback - message received with Id={receivedMessage.MessageId}");
            PrintMessages(receivedMessage);
            await deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"\t{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        /// device twin
        private async Task UpdateTwinData(int ProductionRate, int DeviceError)
        {
            string DeviceErrorString = GetErrorDescription(DeviceError);

            var twin = await deviceClient.GetTwinAsync();
            var reportedProperties = new TwinCollection();

            string ReportedErrorStatus = twin.Properties.Reported.Contains("ErrorStatus") 
                ? twin.Properties.Reported["ErrorStatus"] 
                : "";
            int ReportedProductionRate = twin.Properties.Reported.Contains("ProductionRate") 
                ? twin.Properties.Reported["ProductionRate"] 
                : 0;

            if (!twin.Properties.Reported.Contains("ErrorStatus"))
            {
                reportedProperties["ErrorStatus"] = "";
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            if (!twin.Properties.Reported.Contains("ProductionRate"))
            {
                reportedProperties["ProductionRate"] = 0;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }

            if (!ReportedErrorStatus.Equals(DeviceErrorString))
            {
                reportedProperties["ErrorStatus"] = DeviceErrorString;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Updated ErrorStatus in Device Twin: {DeviceErrorString}");
                
                if (lastErrorState != DeviceError)
                {
                    await SendErrorStateMessage(DeviceError);
                    lastErrorState = DeviceError;
                }
            }
            
            if (ReportedProductionRate != ProductionRate)
            {
                reportedProperties["ProductionRate"] = ProductionRate;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Updated ProductionRate in Device Twin: {ProductionRate}");
            }
        }

        private void PrintMessages(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++} > Key={prop.Key} : Value={prop.Value}");
            }
        }
        #endregion
        #region Direct Methods

        private async Task<MethodResponse> SendMessagesHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { nrOfMessages = default(int), delay = default(int) });
            await SendMessages(payload.nrOfMessages, payload.delay);

            return new MethodResponse(0);
        }

        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");

            await Task.Delay(1000);

            return new MethodResponse(0);
        }

        #endregion
        #region Device Twin

        public async Task UpdateTwinAsync()
        {
            var twin = await deviceClient.GetTwinAsync();

            Console.WriteLine($"\n Initial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            // Sprawdzamy czy Desired Production Rate istnieje
            if (!twin.Properties.Desired.Contains("ProductionRate"))
            {
                Console.WriteLine("\tWarning: Desired Production Rate not set in cloud. Waiting for cloud to set initial value.");
            }

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object _)
        {
            Console.WriteLine($"\tDesired property change\n\t {JsonConvert.SerializeObject(desiredProperties)}");
            
            if (desiredProperties.Contains("ProductionRate"))
            {
                int desiredRate = desiredProperties["ProductionRate"];
                if (desiredRate != lastDesiredProductionRate)
                {
                    await SetProductionRate(desiredRate);
                    lastDesiredProductionRate = desiredRate;
                }
            }

            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task SetProductionRate(int rate)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            
            try
            {
                // Ustawiamy nową wartość Production Rate na maszynie
                var ProductionRateNode = new OpcWriteNode($"ns=2;s={DeviceName}/ProductionRate", rate);
                client.WriteNode(ProductionRateNode);
                
                // Aktualizujemy Device Twin
                var reportedProperties = new TwinCollection();
                reportedProperties["ProductionRate"] = rate;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Set Production Rate to: {rate}%");
            }
            finally
            {
                client.Disconnect();
            }
        }

        #endregion
        public async Task InitializeHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(OnC2MessageReceivedAsync, deviceClient);

            await deviceClient.SetMethodDefaultHandlerAsync(DefaultServiceHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("SendMessages", SendMessagesHandler, deviceClient);

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);
            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ClearErrors", ResetErrors, deviceClient);
            await deviceClient.SetMethodHandlerAsync("SetDeviceStatus", SetDeviceStatus, deviceClient);
        }
        /// emergency
        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            await Task.Delay(1000);
            client.CallMethod($"ns=2;s={DeviceName}", $"ns=2;s={DeviceName}/EmergencyStop");
            
            // Ustawiamy flagę Emergency Stop (bit 0)
            await SetErrorFlag(1);

            client.Disconnect();
            Console.WriteLine("STOP!!!!!!");
            return new MethodResponse(0);
        }
        /// reset errors
        private async Task<MethodResponse> ResetErrors(MethodRequest methodRequest, object userContext)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            await Task.Delay(1000);
            client.CallMethod($"ns=2;s={DeviceName}", $"ns=2;s={DeviceName}/ResetErrorStatus");
            
            // Resetujemy wszystkie flagi błędów
            await SetErrorFlag(0);

            client.Disconnect();
            Console.WriteLine("Errors Reseted =)");
            return new MethodResponse(0);
        }

        private async Task SetErrorFlag(int errorFlag)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            
            try
            {
                // Odczytujemy aktualny stan błędów
                var DeviceError = new OpcReadNode($"ns=2;s={DeviceName}/DeviceError");
                int currentErrorState = client.ReadNode(DeviceError).As<int>();
                
                // Jeśli errorFlag = 0, resetujemy wszystkie flagi
                // W przeciwnym razie ustawiamy odpowiednią flagę
                int newErrorState = errorFlag == 0 ? 0 : (currentErrorState | errorFlag);
                
                // Ustawiamy nowy stan błędów
                var DeviceErrorNode = new OpcWriteNode($"ns=2;s={DeviceName}/DeviceError", newErrorState);
                client.WriteNode(DeviceErrorNode);
                
                // Aktualizujemy Device Twin i wysyłamy wiadomość D2C
                await UpdateTwinData(0, newErrorState);
            }
            finally
            {
                client.Disconnect();
            }
        }

        private async Task SendErrorStateMessage(int errorState)
        {
            var errorData = new
            {
                DeviceId = iotHubDeviceId,
                Timestamp = DateTime.UtcNow,
                ErrorState = errorState,
                ErrorDescription = GetErrorDescription(errorState)
            };

            var errorMessageString = JsonConvert.SerializeObject(errorData);
            Message errorMessage = new Message(Encoding.UTF8.GetBytes(errorMessageString));
            errorMessage.ContentType = MediaTypeNames.Application.Json;
            errorMessage.ContentEncoding = "utf-8";
            errorMessage.Properties.Add("MessageType", "ErrorState");
            
            Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending Error State: [{errorMessageString}]");
            await deviceClient.SendEventAsync(errorMessage);
        }

        private string GetErrorDescription(int errorState)
        {
            List<string> errors = new List<string>();
            
            if ((errorState & 8) != 0) errors.Add("Unknown Error");
            if ((errorState & 4) != 0) errors.Add("Sensor Failure");
            if ((errorState & 2) != 0) errors.Add("Power Failure");
            if ((errorState & 1) != 0) errors.Add("Emergency Stop");
            
            return errors.Count > 0 ? string.Join(", ", errors) : "None";
        }

        private async Task<MethodResponse> SetDeviceStatus(MethodRequest methodRequest, object userContext)
        {
            var client = new OpcClient(OPCstring);
            client.Connect();
            
            try
            {
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Received SetDeviceStatus request: {methodRequest.DataAsJson}");
                var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { isRunning = default(bool) });
                
                var ProductionStatusNode = new OpcWriteNode($"ns=2;s={DeviceName}/ProductionStatus", payload.isRunning ? 1 : 0);
                client.WriteNode(ProductionStatusNode);
                
                // Aktualizujemy Device Twin
                var reportedProperties = new TwinCollection();
                reportedProperties["ProductionStatus"] = payload.isRunning ? 1 : 0;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Set ProductionStatus to: {(payload.isRunning ? "Running (1)" : "Stopped (0)")}");
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Updated Device Twin with new status");
                
                return new MethodResponse(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\tError setting device status: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\tInner exception: {ex.InnerException.Message}");
                }
                return new MethodResponse(500);
            }
            finally
            {
                client.Disconnect();
            }
        }
    }
}
