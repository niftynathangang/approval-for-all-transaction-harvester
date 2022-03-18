# approval-for-all-transaction-harvester

This tool is intended for use to gather all Approval For All ERC721 logs during a specified time period and summarizing the gas usage per user to perform those transactions.  The intent is to filter on specific token contracts, and a specific approved operator address.

## Building

This project can be built without installing Visual Studio or the .Net 5 SDK using Docker.  Here are the instructions to build a Docker image with this application.

```
./approval-for-all-transaction-harvester> cd ApprovalTransactionCollector/ApprovalTransactionCollector
./approval-for-all-transaction-harvester/ApprovalTransactionCollector/ApprovalTransactionCollector> docker build -t approval-transaction-harvester .

> docker image ls --all
## REPOSITORY                         TAG         IMAGE ID       CREATED         SIZE
## approval-transaction-harvester     latest      4fe4e99d74dd   6 seconds ago   88.3MB
```

## Running The Application

Environment variables must be specified to serve as arguments to the app running in the Docker container.

### Environment Variables

| Variable           | Description                                                                                        |
| ------------------ | -------------------------------------------------------------------------------------------------- |
| PROVIDER_URI       | The http URL of your Alchemy/Infura/Other Json-RPC node                                            |
| FROM_BLOCK         | The first block number from which to collect ApprovalForAll logs                                   |
| TO_BLOCK           | The last block number from which collect ApprovalForAll logs                                       |
| BLOCKS_PER_REQUEST | Block page size for log requests - defaults to 2000 per documented Alchemy limitation              |
| OPERATOR_ADDRESS   | The approved operator address to filter on                                                         |
| INPUT_FILE         | JSON file containing token contract addresses of interest to filter on                             |
| OUTPUT_FILE        | JSON file that aggregates and summarizes all users' ApprovalForAll activity and associated gas fes |

### Input File Format

```
{
  "ContractAddresses": [
     "0x2f14f1b6c350c41801b2b7ba9445670d7e2ffc70",
     "ADDRESS 2",
     "ADDRESS 3"
     ...
     "ADDRESS N"
   ]
}
```

### Output File Format

```
{
  "0x5e219Fa598a62E898c6c1b4BFfC2c5bb02D118f7": {
    "UserAddress": "0x5e219Fa598a62E898c6c1b4BFfC2c5bb02D118f7",
    "NumberOfApprovalTransactions": 1,
    "TotalGasFees": 0.005135087540586128,
    "TopFiveGasFeesInEth": 0.005135087540586128,
    "BottomFiveGasFeesInEth": 0.005135087540586128,
    "UserTransactions": [
      {
        "TokenContract": "0x2f14f1b6c350c41801b2b7ba9445670d7e2ffc70",
        "TxHash": "0x69f96cdee2dad5c7b379d9fa9e005a4a1b4762ad15036c649ab237724c375e8b",
        "Owner": "0x5e219Fa598a62E898c6c1b4BFfC2c5bb02D118f7",
        "Operator": "0x6eAECB028049d553dDF0311ad4Cb310c671D79d0",
        "Approved": true,
        "GasUsed": 46162,
        "GasPrice": 111240577544,
        "GasFeeInEth": 0.005135087540586128,
        "BlockNumber": 14404597,
        "BlockTimestamp": 1647527696,
        "UtcDateTime": "2022-03-17T21:34:56Z"
      }
    ]
  },
  "0x7a984C84F0FafadaAb7D0395e6abe560E26Ff370": {
    "UserAddress": "0x7a984C84F0FafadaAb7D0395e6abe560E26Ff370",
    "NumberOfApprovalTransactions": 1,
    "TotalGasFees": 0.004380497290727888,
    "TopFiveGasFeesInEth": 0.004380497290727888,
    "BottomFiveGasFeesInEth": 0.004380497290727888,
    "UserTransactions": [
      {
        "TokenContract": "0x2f14f1b6c350c41801b2b7ba9445670d7e2ffc70",
        "TxHash": "0xe7409508fdd34765741b4cabfc72c6fef5e1ca97ef7f1612be30e814424ab755",
        "Owner": "0x7a984C84F0FafadaAb7D0395e6abe560E26Ff370",
        "Operator": "0x6eAECB028049d553dDF0311ad4Cb310c671D79d0",
        "Approved": true,
        "GasUsed": 46162,
        "GasPrice": 94894010024,
        "GasFeeInEth": 0.004380497290727888,
        "BlockNumber": 14404662,
        "BlockTimestamp": 1647528453,
        "UtcDateTime": "2022-03-17T21:47:33Z"
      }
    ]
  }
}
```

### Sample Docker Command

First, create a directory containing a json input file (contracts.json) that includes all the contracts of interest.  This directory will be mapped to our Docker container as a volume to give our app access to the input file.  This directory is also where we will write the output file.

```
docker run -it -v "$PWD":/data -e PROVIDER_URI=<alchemy_provider_uri> -e FROM_BLOCK=14363277 -e TO_BLOCK=14406477 -e BLOCKS_PER_REQUEST=2000 -e OPERATOR_ADDRESS=0x6eAECB028049d553dDF0311ad4Cb310c671D79d0 -e INPUT_FILE=/data/contracts.json -e OUTPUT_FILE=/data/output.json approval-transaction-harvester
```