using System.Security.Cryptography.X509Certificates;
using Azure.Messaging.ServiceBus;

const string sbConnectionString = "Endpoint=sb://ul-service-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=L2MXgx/3Eqqv+xxaLP+uhQX8ayax7XR7p+ASbPitzzI=";
const string queueName = "myqueue";

await using ServiceBusClient client = new ServiceBusClient(sbConnectionString);
await using ServiceBusProcessor processor = client.CreateProcessor(queueName);


processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

await processor.StartProcessingAsync();

Console.WriteLine("Waiting for messages... Press enter to stop");
Console.ReadLine();
Console.WriteLine("Stopping the receiver...");
await processor.StopProcessingAsync();
Console.WriteLine("Stopped the receiver.");


async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
{
    Console.WriteLine($"RECEIVED MESSAGE: \n\t {arg.Message.Body}");
    await arg.CompleteMessageAsync(arg.Message);
}

Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
{
    Console.WriteLine(arg.Exception.ToString());
    return Task.CompletedTask;
}
