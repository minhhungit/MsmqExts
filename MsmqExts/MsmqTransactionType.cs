namespace MsmqExts
{
    public enum MsmqTransactionType
    {
        /// <summary>
        /// Internal (MSMQ) transaction will be used to fetch message in queue, 
        /// does not support remote queues.
        /// </summary>
        Internal,

        /// <summary>
        /// External (DTC) transaction will be used to fetch message in queue. 
        /// Supports remote queues, but requires running MSDTC Service.
        /// </summary>
        //Dtc
    }
}
