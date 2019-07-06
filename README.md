# MsmqExts <a href="https://www.nuget.org/packages/MsmqExts/"><img src="https://img.shields.io/nuget/v/MsmqExts.svg?style=flat" /> </a>
MSMQ (Microsoft Message Queuing) helper library

### Install
```powershell
Install-Package MsmqExts
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

<img src="https://raw.githubusercontent.com/minhhungit/MsmqExts/master/wiki/producer.png" />

<img src="https://raw.githubusercontent.com/minhhungit/MsmqExts/master/wiki/consumer.png" />


Inspired by https://www.hangfire.io
