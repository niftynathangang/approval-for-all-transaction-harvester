using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;

namespace ApprovalTransactionCollector
{
    class Program
    {
        static async Task Main(string[] args)
        {            
            var web3 = new Web3(Environment.GetEnvironmentVariable("PROVIDER_URI"));
            ulong fromBlock = ulong.Parse(Environment.GetEnvironmentVariable("FROM_BLOCK"));
            ulong toBlock = ulong.Parse(Environment.GetEnvironmentVariable("TO_BLOCK"));
            ulong blocksPerRequest = uint.Parse(Environment.GetEnvironmentVariable("BLOCK_PER_REQUEST") ?? "2000");
            string operatorAddress = Environment.GetEnvironmentVariable("OPERATOR_ADDRESS");
            string inputFile = Environment.GetEnvironmentVariable("INPUT_FILE");
            string outputFile = Environment.GetEnvironmentVariable("OUTPUT_FILE");

            ContractsOfInterest contractsOfInterest = JsonConvert.DeserializeObject<ContractsOfInterest>(File.ReadAllText(inputFile));
            
            Dictionary<string, UserReimbursementSummary> approvalTransactionSummariesByUser = new Dictionary<string, UserReimbursementSummary>();

            for (ulong startBlock = fromBlock; startBlock <= toBlock; startBlock += blocksPerRequest)
            {
                ulong endBlock = startBlock + blocksPerRequest;
                if(endBlock > toBlock)
                {
                    endBlock = toBlock;
                }

                Console.WriteLine($"Retrieving ApprovalForAll Logs From Block {startBlock} To Block {endBlock}");

                List<ApprovalForAllTransactionSummary> approvalTransactionSummaries = await GetAllApprovalForAllEventsOnChainByOperator(contractsOfInterest, web3, startBlock, endBlock, operatorAddress);
                
                foreach (var approvalSummary in approvalTransactionSummaries)
                {
                    if (!approvalTransactionSummariesByUser.TryGetValue(approvalSummary.Owner, out UserReimbursementSummary userSummaries) || userSummaries == null)
                    {
                        userSummaries = new UserReimbursementSummary();
                        userSummaries.UserAddress = approvalSummary.Owner;
                        approvalTransactionSummariesByUser.TryAdd(approvalSummary.Owner, userSummaries);
                    }

                    userSummaries.UserTransactions.Add(approvalSummary);
                }
            }

            Console.WriteLine("Aggregating Results");
            await Task.Delay(1000);

            string json = JsonConvert.SerializeObject(approvalTransactionSummariesByUser, Formatting.Indented);

            Console.WriteLine($"Saving Results To {outputFile}");
            await Task.Delay(1000);

            File.WriteAllText(outputFile, json);        
        }

        private static async Task<List<ApprovalForAllTransactionSummary>> GetAllApprovalForAllEventsOnChainByOperator(ContractsOfInterest contractsOfInterest, Web3 web3, ulong fromBlock, ulong toBlock, string operatorAddress)
        {
            var approvalForAllEventHandlerAnyContract = web3.Eth.GetEvent<ApprovalForAllEventDTO>();
            var filterAllApprovalForAllEventsForAllContracts = approvalForAllEventHandlerAnyContract.CreateFilterInput<string, string>(null, operatorAddress, new BlockParameter(fromBlock), new BlockParameter(toBlock));

            var allApprovalForAllEventsForContract = await approvalForAllEventHandlerAnyContract.GetAllChangesAsync(filterAllApprovalForAllEventsForAllContracts);

            List<ApprovalForAllTransactionSummary> approvalTransactionSummaries = new List<ApprovalForAllTransactionSummary>();
            foreach (var eventDTO in allApprovalForAllEventsForContract)
            {
                if (contractsOfInterest.IsContractOfInterest(eventDTO.Log.Address))
                {
                    var txReceipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(eventDTO.Log.TransactionHash);
                    var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(txReceipt.BlockNumber));
                    var gasFeeWei = txReceipt.GasUsed.Value * txReceipt.EffectiveGasPrice.Value;
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToUniversalTime();
                    DateTime utcDateTime = epoch.AddSeconds(((int)block.Timestamp.Value));
                    approvalTransactionSummaries.Add(new ApprovalForAllTransactionSummary()
                    {
                        TokenContract = eventDTO.Log.Address,
                        TxHash = eventDTO.Log.TransactionHash,
                        Owner = eventDTO.Event.Owner,
                        Operator = eventDTO.Event.Operator,
                        Approved = eventDTO.Event.Approved,
                        GasUsed = txReceipt.GasUsed.Value,
                        GasPrice = txReceipt.EffectiveGasPrice.Value,
                        GasFeeInEth = Web3.Convert.FromWei(gasFeeWei),
                        BlockNumber = txReceipt.BlockNumber.Value,
                        BlockTimestamp = block.Timestamp.Value,
                        UtcDateTime = utcDateTime
                    });
                }
            }

            return approvalTransactionSummaries;
        }
    }
}
