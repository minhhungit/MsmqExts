using System;

namespace SimpleMessage
{
    public class ProductMessage
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Seq { get; set; }
    }
}
