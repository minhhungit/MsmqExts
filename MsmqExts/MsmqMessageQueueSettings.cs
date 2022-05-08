using MsmqExts.Extensions;
using Newtonsoft.Json;
using System;
#if NET462
using System.Messaging;
#else
#endif

namespace MsmqExts
{
    public class MsmqMessageQueueSettings
    {
        public MsmqMessageQueueSettings()
        {
            TransactionType = MsmqTransactionType.Internal;
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver()
            };
        }

        public MsmqTransactionType TransactionType { get; set; }

        /// <summary>
        /// Dequeue timeout, default is 2 seconds
        /// If receive timeout is set to null then it will be set to 2 seconds like default value
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public Action<Exception> LogExceptioAction { get; set; }
        public Action<TimeSpan> LogEnqueueElapsedTimeAction { get; set; }
        public Action<TimeSpan> LogDequeueElapsedTimeAction { get; set; }

        /// <summary>
        /// params:
        /// - elapsed time
        /// - nbr of processed messages
        /// - batch size
        /// </summary>
        public Action<TimeSpan, int, int> LogDequeueBatchElapsedTime { get; set; } // elapsed, nbrOfProcessedMessages, batchsize
    }
}
