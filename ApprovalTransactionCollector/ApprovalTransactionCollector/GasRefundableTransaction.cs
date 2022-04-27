using System.Numerics;

namespace ApprovalTransactionCollector
{
    public class GasRefundableTransactionRecord
    {
        public string Wallet { get; set; }
        public string TxHash { get; set; }
        public string Summary { get; set; }
        public BigInteger GasUsed { get; set; }
        public BigInteger GasPriceWei { get; set; }

        public GasRefundableTransactionRecord()
        {                    
            
        }
    }
}