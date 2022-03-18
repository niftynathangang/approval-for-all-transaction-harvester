using System.Linq;
using System.Collections.Generic;

namespace ApprovalTransactionCollector
{
    public class ContractsOfInterest
    {
        public List<string> ContractAddresses { get; private set; }

        public ContractsOfInterest()
        {
            ContractAddresses = new List<string>();
        }

        public bool IsContractOfInterest(string contractAddress)
        {
            return ContractAddresses.Select(x => x.ToLowerInvariant()).Contains(contractAddress.ToLowerInvariant());
        }
    }
}
