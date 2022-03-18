using Nethereum.ABI.FunctionEncoding.Attributes;

namespace ApprovalTransactionCollector
{
    /// <summary>
    /// DTO for ApprovalForAll events from EIP-721
    ///
    /// event ApprovalForAll(address indexed _owner, address indexed _operator, bool _approved); 
    /// </summary>
    [Event("ApprovalForAll")]
    public class ApprovalForAllEventDTO : IEventDTO
    {
        [Parameter("address", "_owner", 1, true)]
        public string Owner { get; set; }

        [Parameter("address", "_operator", 2, true)]
        public string Operator { get; set; }

        [Parameter("bool", "_approved", 3, false)]
        public bool Approved { get; set; }
    }
}
