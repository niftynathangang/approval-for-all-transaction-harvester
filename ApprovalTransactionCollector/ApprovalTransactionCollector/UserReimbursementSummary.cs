using System.Collections.Generic;
using System.Linq;

namespace ApprovalTransactionCollector
{
    public class UserReimbursementSummary
    {
        public string UserAddress { get; set; }
        public int NumberOfApprovalTransactions { get { return UserTransactions.Count; } }
        public decimal TotalGasFees { get { return UserTransactions?.Sum(x => x.GasFeeInEth) ?? 0.0m; } }
        public decimal TopFiveGasFeesInEth { get { return UserTransactions?.OrderByDescending(x => x.GasFeeInEth).Take(5).Sum(x => x.GasFeeInEth) ?? 0.0m; } }
        public decimal BottomFiveGasFeesInEth { get { return UserTransactions?.OrderBy(x => x.GasFeeInEth).Take(5).Sum(x => x.GasFeeInEth) ?? 0.0m; } }

        public List<ApprovalForAllTransactionSummary> UserTransactions { get; private set; }

        public UserReimbursementSummary()
        {
            UserTransactions = new List<ApprovalForAllTransactionSummary>();
        }
    }
}
