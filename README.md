# MsmqExts <a href="https://www.nuget.org/packages/MsmqExts/"><img src="https://img.shields.io/nuget/v/MsmqExts.svg?style=flat" /> </a>
MSMQ (Microsoft Message Queuing) helper library

### Install
```powershell
Install-Package MsmqExts
```

### Simple Publisher
```csharp
var _messageQueue = new MsmqMessageQueue();
var obj = new Product
{
    Id = 1,
    Name = @"Jin"
};

_messageQueue.Enqueue("my-queue", obj);
```

### Simple Consumer
```csharp
CancellationTokenSource tokenSource = new CancellationTokenSource();
CancellationToken token = tokenSource.Token;

var _messageQueue = new MsmqMessageQueue();

using(var deObj = _messageQueue.Dequeue("my-queue", token))
{
    try
    {
        if (deObj.Result is Product prod)
        {
            Console.WriteLine($"- processing product <{prod.Id}>");
        }
        deObj.Commit();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    
        deObj.Abort();
    }
}
```

### Feature:
- Transaction
- Fetch batch messages

### How to run sample: 

- Create a MSMQ **transaction** queue 
- Update queue name ( `AppConstants.MyQueueName` )
- Run Publisher - Consumer
- Enjoy

### Sample screenshot

<img src="https://raw.githubusercontent.com/minhhungit/MsmqExts/master/wiki/simple-sample-msmqexts.gif" />

<img src="https://raw.githubusercontent.com/minhhungit/MsmqExts/master/wiki/producer.png" />

<img src="https://raw.githubusercontent.com/minhhungit/MsmqExts/master/wiki/consumer.png" />

---

Inspired by https://www.hangfire.io
