using Azure.Messaging.ServiceBus;

const string sbConnectionString = "";
const string queueName = "myqueues";

await using ServiceBusClient client = new ServiceBusClient(sbConnectionString);
await using ServiceBusProcessor processor = client.CreateProcessor(queueName);



processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

await processor.StartProcessingAsync();

Console.Write("Waiting for messages... Press enter to stop.");
Console.ReadLine();

Console.WriteLine("/n Stopping the receiver...");
await processor.StopProcessingAsync();
Console.WriteLine("Stoped receiveing");

async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
{
    Console.WriteLine($"RECEIVED MESSAGE: \n\t { arg.Message.Body}");
    await arg.CompleteMessageAsync(arg.Message);
}

Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
{
    Console.WriteLine(arg.Exception.ToString() );
    return Task.CompletedTask;
}