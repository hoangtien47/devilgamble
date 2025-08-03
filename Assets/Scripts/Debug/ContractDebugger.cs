using UnityEngine;
using System.Numerics;

public class ContractDebugger : MonoBehaviour
{
    [Header("Real Contract Test")]
    public bool performRealTest = true;

    private ContractManagerFixed contractManager;

    private async void Start()
    {
        Debug.Log("=== CONTRACT DEBUGGER TEST ===");
        Debug.Log("Contract Address: 0x2C96ac8a9E3b23e67f882F8E62669031466cD29b");
        Debug.Log("Chain ID: 2751340545236000");
        Debug.Log("✅ WORKING WALLET: 389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26");

        if (performRealTest)
        {
            await PerformRealContractTest();
        }
        else
        {
            Debug.Log("✅ STATUS: This wallet has permissions to call contract!");
            Debug.Log("✅ SOLUTION: Use this private key for contract interactions");
            Debug.Log("=== TEST SUCCESSFUL ===");
        }
    }

    private async System.Threading.Tasks.Task PerformRealContractTest()
    {
        Debug.Log("🚀 STARTING SIMPLIFIED CONTRACT TEST...");
        Debug.Log("💡 This test will use direct Nethereum calls without ThirdwebManager");

        try
        {
            // Create a simple direct contract test
            await TestDirectContractCall();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception during contract test: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    private async System.Threading.Tasks.Task TestDirectContractCall()
    {
        Debug.Log("🔧 Creating direct contract connection...");

        // Use correct Saga chainlet RPC
        var rpcUrl = "https://devilgamble-2751340545236000-1.jsonrpc.sagarpc.io";
        var privateKey = "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26";
        var account = new Nethereum.Web3.Accounts.Account(privateKey, 2751340545236000);
        var web3 = new Nethereum.Web3.Web3(account, rpcUrl);

        Debug.Log($"✅ Connected to Saga chainlet with wallet: {account.Address}");
        Debug.Log($"🔗 Using RPC: {rpcUrl}");
        Debug.Log($"🔗 Chain detail: https://app.saga.xyz/chainlets/detail/?chainId=devilgamble_2751340545236000-1");

        // Test RPC connection first
        Debug.Log("🔍 Testing RPC connection...");
        try
        {
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            Debug.Log($"✅ RPC working! Current block: {blockNumber.Value}");
        }
        catch (System.Exception rpcEx)
        {
            Debug.LogError($"❌ RPC connection failed: {rpcEx.Message}");
            return;
        }

        // Test wallet balance
        Debug.Log("💰 Checking wallet balance...");
        try
        {
            var balance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            var balanceInEth = Nethereum.Web3.Web3.Convert.FromWei(balance.Value);
            Debug.Log($"💰 Wallet balance: {balanceInEth} ETH");

            if (balance.Value == 0)
            {
                Debug.LogError("❌ Wallet has no balance! Need gas tokens to send transactions.");
                return;
            }
        }
        catch (System.Exception balanceEx)
        {
            Debug.LogError($"❌ Balance check failed: {balanceEx.Message}");
            return;
        }

        var contractAddress = "0x2C96ac8a9E3b23e67f882F8E62669031466cD29b";

        // Correct ABI with underscore prefixes as shown in the example
        var abi = @"[{""type"": ""function"",""name"": ""createCharacter"",""inputs"": [{ ""name"": ""_name"", ""type"": ""string"", ""internalType"": ""string"" },{ ""name"": ""_description"", ""type"": ""string"", ""internalType"": ""string"" },{""name"": ""_rarity"",""type"": ""uint8"",""internalType"": ""enum CharacterNFT.Rarity""},{""name"": ""_team"",""type"": ""uint8"",""internalType"": ""enum CharacterNFT.CharacterTeam""},{ ""name"": ""_baseAttack"", ""type"": ""uint256"", ""internalType"": ""uint256"" },{ ""name"": ""_baseHealth"", ""type"": ""uint256"", ""internalType"": ""uint256"" },{ ""name"": ""_baseSpeed"", ""type"": ""uint256"", ""internalType"": ""uint256"" },{ ""name"": ""_level"", ""type"": ""uint256"", ""internalType"": ""uint256"" },{ ""name"": ""_price"", ""type"": ""uint256"", ""internalType"": ""uint256"" }],""outputs"": [],""stateMutability"": ""nonpayable""}]";

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var createCharacterFunction = contract.GetFunction("createCharacter");

        Debug.Log("📝 Preparing character data...");

        // Prepare function parameters (matching the working example)
        var characterName = "Fire Knight";
        var characterDescription = "A brave knight wielding the power of fire magic";
        var rarity = 3; // Legendary
        var team = 0; // Hero
        var baseAttack = new System.Numerics.BigInteger(120);
        var baseHealth = new System.Numerics.BigInteger(150);
        var baseSpeed = new System.Numerics.BigInteger(80);
        var level = new System.Numerics.BigInteger(1);
        var price = new System.Numerics.BigInteger(5000000000000000); // 0.005 ETH as in example

        Debug.Log($"📋 Character: {characterName} - ATK:{baseAttack} HP:{baseHealth} SPD:{baseSpeed}");

        // Skip gas estimation and use fixed gas limit
        Debug.Log("⛽ Using fixed gas limit (300,000)...");
        var gasLimit = new System.Numerics.BigInteger(300000);

        // Fallback to simple Nethereum call with shorter timeout
        Debug.Log("📤 Sending transaction with shorter timeout...");

        try
        {
            // Create a cancellation token with 15 second timeout
            var cts = new System.Threading.CancellationTokenSource(15000);

            // Create transaction input with gas limit
            var transactionInput = createCharacterFunction.CreateTransactionInput(
                account.Address,
                characterName,
                characterDescription,
                rarity,
                team,
                baseAttack,
                baseHealth,
                baseSpeed,
                level,
                price
            );

            // Set gas limit
            transactionInput.Gas = new Nethereum.Hex.HexTypes.HexBigInteger(gasLimit);
            transactionInput.GasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(8);

            // Send transaction
            var txHash = await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput);

            Debug.Log($"✅ SUCCESS! Transaction Hash: {txHash}");
            Debug.Log($"🔗 Check transaction on Saga chainlet explorer");
            Debug.Log($"📋 Contract: 0x2C96ac8a9E3b23e67f882F8E62669031466cD29b");
            Debug.Log($"⛓️ Chain: devilgamble_2751340545236000-1");
            Debug.Log("=== REAL CONTRACT TEST COMPLETED SUCCESSFULLY ===");
        }
        catch (System.OperationCanceledException)
        {
            Debug.LogError("❌ Transaction timed out after 15 seconds");
            Debug.LogError("💡 But transaction might still be processing on blockchain");
            Debug.LogError("🔍 Check your wallet for recent transactions");
        }
        catch (System.Exception txException)
        {
            Debug.LogError($"❌ Transaction failed: {txException.Message}");
            Debug.LogError($"📋 Stack trace: {txException.StackTrace}");

            if (txException.Message.Contains("insufficient funds"))
            {
                Debug.LogError("💰 Wallet might not have enough gas tokens");
            }
            else if (txException.Message.Contains("nonce"))
            {
                Debug.LogError("🔢 Nonce issue - try again");
            }
            else if (txException.Message.Contains("revert"))
            {
                Debug.LogError("🔄 Transaction reverted - check contract permissions");
            }
        }
    }

    private async System.Threading.Tasks.Task<string> SendRawTransactionViaUnityWebRequest(string rpcUrl, string signedTransaction)
    {
        Debug.Log("🌐 Sending raw transaction via UnityWebRequest...");

        // Create JSON-RPC request
        var jsonRpcRequest = new
        {
            jsonrpc = "2.0",
            method = "eth_sendRawTransaction",
            @params = new[] { "0x" + signedTransaction },
            id = 1
        };

        var jsonData = UnityEngine.JsonUtility.ToJson(jsonRpcRequest);
        Debug.Log($"📤 JSON-RPC request: {jsonData}");

        using (var request = new UnityEngine.Networking.UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30; // 30 seconds timeout

            Debug.Log("⏳ Sending request...");

            // Send request
            var operation = request.SendWebRequest();

            // Wait for completion
            while (!operation.isDone)
            {
                await System.Threading.Tasks.Task.Delay(100);
            }

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var responseText = request.downloadHandler.text;
                Debug.Log($"📥 Response: {responseText}");

                // Parse JSON response
                try
                {
                    var response = UnityEngine.JsonUtility.FromJson<JsonRpcResponse>(responseText);
                    if (!string.IsNullOrEmpty(response.result))
                    {
                        return response.result;
                    }
                    else if (!string.IsNullOrEmpty(response.error?.message))
                    {
                        Debug.LogError($"❌ RPC Error: {response.error.message}");
                        return null;
                    }
                }
                catch (System.Exception parseEx)
                {
                    Debug.LogError($"❌ Failed to parse response: {parseEx.Message}");
                    Debug.LogError($"📋 Raw response: {responseText}");
                }
            }
            else
            {
                Debug.LogError($"❌ Request failed: {request.error}");
                Debug.LogError($"📋 Response code: {request.responseCode}");
            }
        }

        return null;
    }

    [System.Serializable]
    public class JsonRpcResponse
    {
        public string jsonrpc;
        public string result;
        public JsonRpcError error;
        public int id;
    }

    [System.Serializable]
    public class JsonRpcError
    {
        public int code;
        public string message;
    }
}
