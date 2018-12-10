using System;

namespace idsip_terminal
{
    class Messages
    {
        public string Command { get; set; }
        public object Args { get; set; }
    }

    class Transaction
    {
        public string TransactionID { get; set; }
        public string LP { get; set; }
        public DateTime Time { get; set; }
        public double Price { get; set; }
    }

    class Payment
    {
        public double Price { get; set; }
    }
}
