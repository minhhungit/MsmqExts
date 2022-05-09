# MsmqExts <a href="https://www.nuget.org/packages/MsmqExts/"><img src="https://img.shields.io/nuget/v/MsmqExts.svg?style=flat" /> </a>
MSMQ (Microsoft Message Queuing) helper library

### Install
```powershell
Install-Package MsmqExts
```

### Performance

## Single message en-queue ##
```
Enqueued a message in 0.45ms
Enqueued a message in 0.80ms
Enqueued a message in 0.66ms
Enqueued a message in 0.54ms
Enqueued a message in 0.46ms
```
## Single message de-queue ##
```
Dequeue a message in 0.11ms
Dequeue a message in 0.15ms
Dequeue a message in 0.11ms
Dequeue a message in 0.11ms
Dequeue a message in 0.11ms
```
## Batch messages en-queue ##
```
Enqueued a batch 50000 message(s) in 2454.79ms, avg 0.05ms per message
Enqueued a batch 50000 message(s) in 1986.63ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 1990.47ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 2067.62ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 2147.41ms, avg 0.04ms per message
```
## Batch messages de-queue ##
```
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.05ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.07ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
```
## Demo
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

> [Check more samples here](https://github.com/minhhungit/MsmqExts/tree/main/Sample)

### Feature:
- Transaction
- Batch enqueue/dequeue messages
- Message persistence in mind

### How to run sample: 

- Create a MSMQ **transaction** queue 
- Update queue name ( `AppConstants.MyQueueName` )
- Run Publisher - Consumer
- Enjoy

---

Inspired by https://www.hangfire.io
