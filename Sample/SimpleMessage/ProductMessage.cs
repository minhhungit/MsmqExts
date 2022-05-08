using System;

namespace SimpleMessage
{
    public class ProductMessage
    {
        public ProductMessage(Guid id, DateTime createDate, int seq)
        {
            Id = id;
            CreatedDate = createDate;
            Seq = seq;
        }

        public Guid Id { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public int Seq { get; private set; }
    }
}
