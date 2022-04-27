using System;
using System.Numerics;

namespace ApprovalTransactionCollector
{
    public class ApprovalForAllTransactionSummary
    {
        public string TokenContract { get; set; }
        public string TxHash { get; set; }
        public string Owner { get; set; }
        public string Operator { get; set; }
        public bool Approved { get; set; }
        public BigInteger GasUsed { get; set; }
        public BigInteger GasPrice { get; set; }
        public decimal GasFeeInEth { get; set; }        

        public ApprovalForAllTransactionSummary()
        {

        }
    }
}
