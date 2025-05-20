using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;


const string connectionString = "DefaultEndpointsProtocol=https;AccountName=ulstoragefiles;AccountKey=tGY8Vxx9lKPtw0OKMYm8A2+lzHy9yAvBinr1sJVV3nFBMN0nOg79u61TX+c8of+UWJtrdxsimvSZ+AStcF15VQ==;EndpointSuffix=core.windows.net";

QueueClient queueClient = new QueueClient(connectionString, "myqueue");

await queueClient.CreateIfNotExistsAsync();

await Task.Delay(5000);


QueueMessage[] receivedMessages = await queueClient.ReceiveMessagesAsync(32);

foreach (QueueMessage message in receivedMessages)
{
    Console.WriteLine($"Received message: \n \t {message.Body}");
    // Process the message and then delete it from the queue.
    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
}


Console.WriteLine("\nFINISHED!");
Console.WriteLine("Press any key to exit.");
Console.ReadLine();