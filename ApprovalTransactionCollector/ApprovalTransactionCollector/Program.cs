using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

namespace ApprovalTransactionCollector
{
    class Program
    {
        static async Task Main(string[] args)
        {            
            var web3 = new Web3(Environment.GetEnvironmentVariable("PROVIDER_URI"));
            ulong fromBlock = ulong.Parse(Environment.GetEnvironmentVariable("FROM_BLOCK"));
            ulong toBlock = ulong.Parse(Environment.GetEnvironmentVariable("TO_BLOCK"));
            ulong blocksPerRequest = uint.Parse(Environment.GetEnvironmentVariable("BLOCKS_PER_REQUEST") ?? "2000");
            string operatorAddress = Environment.GetEnvironmentVariable("OPERATOR_ADDRESS");            
            string outputFile = Environment.GetEnvironmentVariable("OUTPUT_FILE") ?? "output.csv";
                       
            Dictionary<string, List<ApprovalForAllTransactionSummary>> approvalTransactionSummariesByUser = new Dictionary<string, List<ApprovalForAllTransactionSummary>>();            

            for (ulong startBlock = fromBlock; startBlock <= toBlock; startBlock += (blocksPerRequest + 1))
            {
                ulong endBlock = startBlock + blocksPerRequest;
                if (endBlock > toBlock)
                {
                    endBlock = toBlock;
                }

                Console.WriteLine($"Retrieving ApprovalForAll Logs From Block {startBlock} To Block {endBlock}");

                List<ApprovalForAllTransactionSummary> approvalTransactionSummaries = await GetAllApprovalForAllEventsOnChainByOperator(web3, startBlock, endBlock, operatorAddress);                

                foreach (var approvalSummary in approvalTransactionSummaries)
                {
                    if (!approvalTransactionSummariesByUser.TryGetValue(approvalSummary.Owner, out List<ApprovalForAllTransactionSummary> userSummaries) || userSummaries == null)
                    {
                        userSummaries = new List<ApprovalForAllTransactionSummary>();                        
                        approvalTransactionSummariesByUser.TryAdd(approvalSummary.Owner, userSummaries);
                    }

                    userSummaries.Add(approvalSummary);
                }
            }

            List<GasRefundableTransactionRecord> transactionsToRefund = new List<GasRefundableTransactionRecord>();
            foreach (var entry in approvalTransactionSummariesByUser)
            {
                string user = entry.Key;
                List<ApprovalForAllTransactionSummary> userApprovalTransactions = entry.Value;
                List<GasRefundableTransactionRecord> userTransactionsToRefund = userApprovalTransactions
                    .OrderBy(x => x.GasFeeInEth)
                    .Select(x => new GasRefundableTransactionRecord()
                    {
                        Wallet = user,
                        TxHash = x.TxHash,
                        Summary = "W2W Approval Promotional Gas Refund",
                        GasUsed = x.GasUsed,
                        GasPriceWei = x.GasPrice
                    })
                    .Take(3)
                    .ToList();

                if(userTransactionsToRefund.Count >= 3)
                {
                    transactionsToRefund.AddRange(userTransactionsToRefund);
                }
            }

            StringBuilder csvBuilder = new StringBuilder();
            foreach(GasRefundableTransactionRecord record in transactionsToRefund)
            {
                csvBuilder.AppendLine($"{record.Wallet},{record.TxHash},{record.Summary},{record.GasUsed},{record.GasPriceWei}");
            }

            Console.WriteLine("Aggregating Results");
            await Task.Delay(1000);

            //string json = JsonConvert.SerializeObject(approvalTransactionSummariesByUser, Formatting.Indented);
            string csv = csvBuilder.ToString();

            Console.WriteLine($"Saving Results To {outputFile}");
            await Task.Delay(1000);

            //File.WriteAllText(outputFile, json);
            File.WriteAllText(outputFile, csv);
        }

        private static async Task<List<ApprovalForAllTransactionSummary>> GetAllApprovalForAllEventsOnChainByOperator(Web3 web3, ulong fromBlock, ulong toBlock, string operatorAddress)
        {
            var approvalForAllEventHandlerAnyContract = web3.Eth.GetEvent<ApprovalForAllEventDTO>();
            var filterAllApprovalForAllEventsForAllContracts = approvalForAllEventHandlerAnyContract.CreateFilterInput<string, string>(null, operatorAddress, new BlockParameter(fromBlock), new BlockParameter(toBlock));

            var allApprovalForAllEventsForContract = await approvalForAllEventHandlerAnyContract.GetAllChangesAsync(filterAllApprovalForAllEventsForAllContracts);

            List<ApprovalForAllTransactionSummary> approvalTransactionSummaries = new List<ApprovalForAllTransactionSummary>();
            foreach (var eventDTO in allApprovalForAllEventsForContract)
            {                
                if (eventDTO.Event.Approved)
                {
                    string code = await web3.Eth.GetCode.SendRequestAsync(eventDTO.Event.Owner);
                    if(code.ToLowerInvariant().Equals("0x"))
                    {
                        var txReceipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(eventDTO.Log.TransactionHash);                        
                        var gasFeeWei = txReceipt.GasUsed.Value * txReceipt.EffectiveGasPrice.Value;                        
                        approvalTransactionSummaries.Add(new ApprovalForAllTransactionSummary()
                        {
                            TokenContract = eventDTO.Log.Address,
                            TxHash = eventDTO.Log.TransactionHash,
                            Owner = eventDTO.Event.Owner,
                            Operator = eventDTO.Event.Operator,
                            Approved = eventDTO.Event.Approved,
                            GasUsed = txReceipt.GasUsed.Value,
                            GasPrice = txReceipt.EffectiveGasPrice.Value,
                            GasFeeInEth = Web3.Convert.FromWei(gasFeeWei)                            
                        });
                    }                                        
                }
            }

            return approvalTransactionSummaries;
        }
    }
}
