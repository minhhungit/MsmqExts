using System;

namespace MsmqExts
{
    public enum DequeueResultStatus
    {
        Success = 1,
        Timeout = 2,
        Exception = 4
    }

    public interface IFetchedMessage : IDisposable
    {
        void Commit();
        void Abort();
        string Label { get; }
        object Result { get; }
        DequeueResultStatus DequeueResultStatus { get; }
        Exception DequeueException { get; }
    }

    public class MsmqFetchedMessage : IFetchedMessage
    {
        private readonly IMsmqTransaction _transaction;

        public MsmqFetchedMessage(IMsmqTransaction transaction, string label, object result, DequeueResultStatus dequeueResultStatus, Exception dequeueEx)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new Exception("Label should not null or empty");
            }

            if (dequeueResultStatus == DequeueResultStatus.Success)
            {
                _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
                Label = label ?? throw new ArgumentNullException(nameof(label));
                Result = result ?? throw new ArgumentNullException(nameof(result));
            }

            DequeueResultStatus = dequeueResultStatus;
            DequeueException = dequeueEx;
        }

        public string Label { get; private set; }

        /// <summary>
        /// Message object
        /// </summary>
        public object Result { get; private set; }

        public DequeueResultStatus DequeueResultStatus { get; private set; }

        public Exception DequeueException { get; private set; }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Abort()
        {
            _transaction.Abort();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
