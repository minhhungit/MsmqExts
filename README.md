# MsmqExts <a href="https://www.nuget.org/packages/MsmqExts/"><img src="https://img.shields.io/nuget/v/MsmqExts.svg?style=flat" /> </a>
MSMQ (Microsoft Message Queuing) helper library

### Install
```powershell
Install-Package MsmqExts
```

### Performance
>Note: this test is on latest release candidate versions

#### Single message producer ####
```
Enqueued 1 message in 0.26ms
Enqueued 1 message in 0.26ms
Enqueued 1 message in 0.27ms
Enqueued 1 message in 0.28ms
Enqueued 1 message in 0.29ms
Enqueued 1 message in 0.27ms
```

### Single message producer using C# Parallel ###
```
Enqueued 10000 messages(s) in 975.02ms, avg 0.1ms per message
Enqueued 10000 messages(s) in 877.55ms, avg 0.09ms per message
Enqueued 10000 messages(s) in 866.62ms, avg 0.09ms per message
Enqueued 10000 messages(s) in 928.88ms, avg 0.09ms per message
Enqueued 10000 messages(s) in 909.85ms, avg 0.09ms per message
```

#### Single message consumer ####
```
Got a product in 0.12ms | [<11806 / 998a6e0> - 21:12:12.710]
Got a product in 0.18ms | [<11807 / a0e25a3> - 21:12:12.710]
Got a product in 0.14ms | [<11808 / fc7c5e0> - 21:12:12.710]
Got a product in 0.15ms | [<11809 / 23bd871> - 21:12:12.710]
Got a product in 0.12ms | [<11810 / 5e8cb24> - 21:12:12.710]
Got a product in 0.14ms | [<11811 / 99d2df1> - 21:12:12.710]
```
#### Batch messages producer ####
```
Enqueued a batch 50000 message(s) in 1853.38ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 1750.66ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 1796.26ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 1927.82ms, avg 0.04ms per message
Enqueued a batch 50000 message(s) in 1916.92ms, avg 0.04ms per message
```
#### Batch messages consumer ####
```
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
Tried to fetch a batch 10000 messages, got 10000/10000, avg 0.04ms per message
```
## Demo
### Simple Publisher
```csharp
var messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");
var obj = new Product
{
    Id = 1,
    Name = @"Jin"
};

messageQueue.Enqueue(obj);
```

### Simple Consumer
```csharp
CancellationTokenSource tokenSource = new CancellationTokenSource();
CancellationToken token = tokenSource.Token;

var msmqMessageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

try
{
    var message = _messageQueue.Dequeue(token);
    if (message.Result is Product prod)
    {
        Console.WriteLine($"- processing product <{prod.Id}>");
    }
    message.Commit();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    message.Abort();
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
