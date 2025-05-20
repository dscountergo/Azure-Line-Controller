using Azure.Storage.Queues;


const string connectionString = "DefaultEndpointsProtocol=https;AccountName=ulstoragefiles;AccountKey=tGY8Vxx9lKPtw0OKMYm8A2+lzHy9yAvBinr1sJVV3nFBMN0nOg79u61TX+c8of+UWJtrdxsimvSZ+AStcF15VQ==;EndpointSuffix=core.windows.net";

QueueClient queueClient = new QueueClient(connectionString, "myqueue");

await queueClient.CreateIfNotExistsAsync();

for (int i = 0; i < 10; i++)
{
    var message = $"This is message nr {i} created on {DateTime.UtcNow}";

    Console.WriteLine($"Sending message: \n \t {message}");

    await queueClient.SendMessageAsync(message);
    await Task.Delay(200);

}

Console.WriteLine("All messages sent. Press any key to exit.");
Console.ReadLine();


