using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using Thirdweb.Unity;
using UnityEngine;

/// <summary>
/// Fixed version of ContractManager that addresses the OwnableUnauthorizedAccount issue
/// </summary>
public class ContractManagerFixed : MonoBehaviour
{
    private string _characterAbiJson;
    private string _rewardAbiJson;
    private ulong ActiveChainId = 2751340545236000;

    private string _rewardContractAddress = "0x1b2f3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b";
    private string _characterContractAddress = "0x2C96ac8a9E3b23e67f882F8E62669031466cD29b";

    [Header("Wallet Configuration")]
    [Tooltip("Use this if you have the contract owner's private key")]
    public string ownerPrivateKey = "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26";

    [Tooltip("Use this if you have a different authorized wallet's private key")]
    public string authorizedPrivateKey = "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26";

    [Tooltip("If true, will attempt to set the current wallet as game manager (requires owner permissions)")]
    public bool autoSetGameManager = false;

    private void Awake()
    {
        TextAsset abiAsset = Resources.Load<TextAsset>("GameReward");
        _rewardAbiJson = abiAsset.text;

        TextAsset characterAbiAsset = Resources.Load<TextAsset>("CharacterNFT");
        _characterAbiJson = characterAbiAsset.text;
    }

    /// <summary>
    /// Creates a wallet with proper permissions for contract interaction
    /// </summary>
    private async Task<IThirdwebWallet> CreateAuthorizedWallet()
    {
        var client = ThirdwebClient.Create(secretKey: "VLt7vqX-8Vj6ZDkNR_r-bFoFhbBJgYPKkGMLMe0WAKZiyy-tRD4mdgp_UoDKACNlJWQi_u_KZ4UJM9KF5rAbsA");
        
        // Try to use owner private key first
        if (!string.IsNullOrEmpty(ownerPrivateKey))
        {
            Debug.Log("Using owner private key for wallet creation");
            return await PrivateKeyWallet.Create(client, ownerPrivateKey);
        }
        
        // Try authorized private key
        if (!string.IsNullOrEmpty(authorizedPrivateKey))
        {
            Debug.Log("Using authorized private key for wallet creation");
            return await PrivateKeyWallet.Create(client, authorizedPrivateKey);
        }
        
        // Fall back to original private key
        Debug.LogWarning("Using original private key - this may not have sufficient permissions");
        return await PrivateKeyWallet.Create(client, "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26");
    }

    /// <summary>
    /// Sets the current wallet as game manager (requires owner permissions)
    /// </summary>
    public async Task<bool> SetGameManager(string gameManagerAddress)
    {
        try
        {
            var ownerWallet = await CreateAuthorizedWallet();
            var contract = await ThirdwebManager.Instance.GetContract(
                address: _characterContractAddress,
                chainId: ActiveChainId,
                abi: _characterAbiJson);

            Debug.Log($"Setting game manager to: {gameManagerAddress}");

            var transactionReceipt = await contract.Write(
                ownerWallet,
                "setGameManager",
                BigInteger.Zero,
                new object[] { gameManagerAddress }
            );

            Debug.Log($"Game manager set successfully. Tx Hash: {transactionReceipt.TransactionHash}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set game manager: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks contract permissions and suggests solutions
    /// </summary>
    public async Task<bool> DiagnosePermissions()
    {
        try
        {
            var wallet = await CreateAuthorizedWallet();
            string walletAddress = await wallet.GetAddress();
            
            var contract = await ThirdwebManager.Instance.GetContract(
                address: _characterContractAddress,
                chainId: ActiveChainId,
                abi: _characterAbiJson);

            Debug.Log("=== PERMISSION DIAGNOSIS ===");
            Debug.Log($"Current Wallet: {walletAddress}");

            // Check owner
            var owner = await contract.Read<string>("owner");
            bool isOwner = string.Equals(walletAddress, owner, StringComparison.OrdinalIgnoreCase);
            Debug.Log($"Contract Owner: {owner}");
            Debug.Log($"Is Owner: {isOwner}");

            // Check game manager
            var gameManager = await contract.Read<string>("gameManager");
            bool isGameManager = string.Equals(walletAddress, gameManager, StringComparison.OrdinalIgnoreCase);
            Debug.Log($"Game Manager: {gameManager}");
            Debug.Log($"Is Game Manager: {isGameManager}");

            // Check paused state
            var isPaused = await contract.Read<bool>("paused");
            Debug.Log($"Contract Paused: {isPaused}");

            if (isOwner || isGameManager)
            {
                Debug.Log("✅ Wallet has sufficient permissions!");
                return true;
            }
            else
            {
                Debug.LogError("❌ Wallet lacks permissions!");
                Debug.LogError("SOLUTIONS:");
                Debug.LogError("1. Use the contract owner's private key");
                Debug.LogError("2. Have the owner set this wallet as game manager");
                Debug.LogError("3. Use a wallet that is already set as game manager");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Permission diagnosis failed: {e.Message}");
            return false;
        }
    }

    public async Task<List<CharacterCardData>> GetAllCharacterNFT()
    {
        var characterList = new List<CharacterCardData>();
        var result = await ReadContract(_characterContractAddress, _characterAbiJson, "getAllCharacters");
        return ConvertToCharacterCardDataList(result);
    }

    public async Task<string> CreateCharacter(CharacterCardData cardData)
    {
        try
        {
            // First diagnose permissions
            bool hasPermissions = await DiagnosePermissions();
            if (!hasPermissions)
            {
                Debug.LogError("Cannot create character: insufficient permissions");
                return null;
            }

            var wallet = await CreateAuthorizedWallet();
            string activeWalletAddress = await wallet.GetAddress();
            Debug.Log($"Using Wallet Address: {activeWalletAddress}");

            // Auto-set game manager if requested and we have owner permissions
            if (autoSetGameManager)
            {
                await SetGameManager(activeWalletAddress);
            }

            BigInteger weiValue = BigInteger.Zero;

            var contract = await ThirdwebManager.Instance.GetContract(
                address: _characterContractAddress,
                chainId: ActiveChainId,
                abi: _characterAbiJson);

            string _name = cardData.characterName;
            string _description = cardData.characterDescription;
            int _rarity = (int)cardData.rarity;
            int _team = (int)cardData.characterTeam;
            BigInteger _baseAttack = cardData.baseAttack;
            BigInteger _baseHealth = cardData.baseHealth;
            BigInteger _baseSpeed = cardData.baseSpeed;
            BigInteger _level = cardData.level;
            BigInteger _price = cardData.price;

            Debug.Log("Creating character with parameters:");
            Debug.Log($"Name: {_name}, Description: {_description}");
            Debug.Log($"Rarity: {_rarity}, Team: {_team}");
            Debug.Log($"Stats: ATK={_baseAttack}, HP={_baseHealth}, SPD={_baseSpeed}");
            Debug.Log($"Level: {_level}, Price: {_price}");

            var transactionReceipt = await contract.Write(
                wallet,
                "createCharacter",
                weiValue,
                new object[]
                {
                    _name,
                    _description,
                    _rarity,
                    _team,
                    _baseAttack,
                    _baseHealth,
                    _baseSpeed,
                    _level,
                    _price
                }
            );

            Debug.Log("✅ Character created successfully! Tx Hash: " + transactionReceipt.TransactionHash);
            return transactionReceipt.TransactionHash;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating character: {e.Message}\n{e.StackTrace}");

            if (e.Message.Contains("OwnableUnauthorizedAccount"))
            {
                Debug.LogError("This error confirms the wallet lacks owner/game manager permissions!");
                Debug.LogError("Please use DiagnosePermissions() to get specific guidance.");
            }
            return null;
        }
    }

    // Keep existing methods for compatibility
    public async Task PurchaseCharacter(CharacterCardData cardData)
    {
        // Implementation remains the same as original
        try
        {
            var wallet = await CreateAuthorizedWallet();
            string activeWalletAddress = await wallet.GetAddress();
            Debug.Log($"Wallet Address: {activeWalletAddress}");
            BigInteger weiValue = BigInteger.Zero;

            var contract = await ThirdwebManager.Instance.GetContract(
                address: _characterContractAddress,
                chainId: ActiveChainId,
                abi: _characterAbiJson);

            BigInteger _price = cardData.price;
            BigInteger id = new BigInteger(1);

            var transactionReceipt = await contract.Write(
                wallet,
                "purchaseCharacter",
                _price,
                new object[] { id }
            );

            Debug.Log("Character purchased. Tx Hash: " + transactionReceipt.TransactionHash);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error purchasing character: {e.Message}\n{e.StackTrace}");
        }
    }

    public async Task<object[]> ReadContract(string contractAddress, string abiJson, string functionName, object[] parameters = null)
    {
        try
        {
            var contract = await ThirdwebManager.Instance.GetContract(
                address: contractAddress,
                chainId: ActiveChainId,
                abi: abiJson);

            var result = await contract.Read<object[]>(
                functionName,
                parameters
            );
            
            var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
            Debug.Log("Full Result:\n" + jsonResult);

            var characterIds = ((JArray)JToken.FromObject(result[0])).ToObject<List<ulong>>();
            Debug.Log("Character IDs: " + string.Join(", ", characterIds));

            var characterDataList = (JArray)JToken.FromObject(result[1]);

            for (int i = 0; i < characterDataList.Count; i++)
            {
                var character = characterDataList[i];
                Debug.Log($"Character {i + 1}:\n" +
                          $"  Name: {character[0]}\n" +
                          $"  Desc: {character[1]}\n" +
                          $"  Rarity: {character[2]}\n" +
                          $"  Team: {character[3]}\n" +
                          $"  Attack: {character[4]}\n" +
                          $"  Health: {character[5]}\n" +
                          $"  Speed: {character[6]}\n" +
                          $"  Level: {character[7]}\n" +
                          $"  Price: {character[8]}");
            }
            return result;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading contract: {e.Message}");
            return null;
        }
    }

    public List<CharacterCardData> ConvertToCharacterCardDataList(object[] result)
    {
        var characterList = new List<CharacterCardData>();

        if (result == null || result.Length < 2)
        {
            Debug.LogWarning("Invalid result from contract.");
            return characterList;
        }

        var characterDataArray = JArray.FromObject(result[1]);

        foreach (var c in characterDataArray)
        {
            try
            {
                var card = new CharacterCardData
                {
                    characterName = c[0]?["Result"]?.ToString(),
                    characterDescription = c[1]?["Result"]?.ToString(),
                    rarity = (Rarity)(int)c[2]?["Result"]!.ToObject<int>(),
                    characterTeam = (CharacterTeam)(int)c[3]?["Result"]!.ToObject<int>(),
                    baseAttack = c[4]?["Result"]!.ToObject<int>() ?? 0,
                    baseHealth = c[5]?["Result"]!.ToObject<int>() ?? 0,
                    baseSpeed = c[6]?["Result"]!.ToObject<int>() ?? 0,
                    level = c[7]?["Result"]!.ToObject<int>() ?? 0,
                    price = c[8]?["Result"]!.ToObject<BigInteger>() ?? BigInteger.Zero,

                    currentAttack = c[4]?["Result"]!.ToObject<int>() ?? 0,
                    currentHealth = c[5]?["Result"]!.ToObject<int>() ?? 0,
                    currentSpeed = c[6]?["Result"]!.ToObject<int>() ?? 0,
                };

                characterList.Add(card);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse character: {e.Message}");
            }
        }

        return characterList;
    }
}
