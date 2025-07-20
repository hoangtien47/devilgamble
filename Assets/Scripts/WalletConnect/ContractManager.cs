using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using Thirdweb.Unity;
using UnityEngine;


public class ContractManager : MonoBehaviour
{

    private string _characterAbiJson;
    private string _rewardAbiJson;
    private ulong ActiveChainId = 2751340545236000;

    private string _rewardContractAddress = "0x1b2f3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b";
    private string _characterContractAddress = "0x2C96ac8a9E3b23e67f882F8E62669031466cD29b";



    private void Awake()
    {
        TextAsset abiAsset = Resources.Load<TextAsset>("GameReward");
        _rewardAbiJson = abiAsset.text;

        TextAsset characterAbiAsset = Resources.Load<TextAsset>("CharacterNFT");
        _characterAbiJson = characterAbiAsset.text;
    }

    public async Task<List<CharacterCardData>> GetAllCharacterNFT()
    {
        var characterList = new List<CharacterCardData>();
        var result = await ReadContract(_characterContractAddress, _characterAbiJson, "getAllCharacters");

        return ConvertToCharacterCardDataList(result);
    }

    public async Task CreateCharacter(CharacterCardData cardData)
    {
        try
        {
            var client = ThirdwebClient.Create(secretKey: "VLt7vqX-8Vj6ZDkNR_r-bFoFhbBJgYPKkGMLMe0WAKZiyy-tRD4mdgp_UoDKACNlJWQi_u_KZ4UJM9KF5rAbsA");
            var wallet = await PrivateKeyWallet.Create(client, "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26");
            string activeWalletAddress = await wallet.GetAddress();
            Debug.Log($"Wallet Address: {activeWalletAddress}");
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

            // The Write method returns a ThirdwebTransactionReceipt object
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

            // Access the TransactionHash property from the receipt
            Debug.Log("Character created. Tx Hash: " + transactionReceipt.TransactionHash);
        }
        catch (Exception e)
        {
            // Added StackTrace for more detailed error information
            Debug.LogError($"Error creating character: {e.Message}\n{e.StackTrace}");
        }
    }

    public async Task PurchaseCharacter(CharacterCardData cardData)
    {
        try
        {
            var client = ThirdwebClient.Create(secretKey: "VLt7vqX-8Vj6ZDkNR_r-bFoFhbBJgYPKkGMLMe0WAKZiyy-tRD4mdgp_UoDKACNlJWQi_u_KZ4UJM9KF5rAbsA");
            var wallet = await PrivateKeyWallet.Create(client, "389c5d956af04b61895d35af9fcd33b0f93140e71725906848a989d523542d26");
            string activeWalletAddress = await wallet.GetAddress();
            Debug.Log($"Wallet Address: {activeWalletAddress}");
            BigInteger weiValue = BigInteger.Zero;

            var contract = await ThirdwebManager.Instance.GetContract(
                address: _characterContractAddress,
                chainId: ActiveChainId,
                abi: _characterAbiJson);

            BigInteger _price = cardData.price;
            BigInteger id = new BigInteger(1);

            var character = new
            {
                characterId = BigInteger.Parse("1")
            };

            string json = JsonConvert.SerializeObject(character);



            // The Write method returns a ThirdwebTransactionReceipt object
            var transactionReceipt = await contract.Write(
                wallet,
                "purchaseCharacter",
                _price,
                new object[] { id } //  WRAPPED IN OBJECT ARRAY
            );

            // Access the TransactionHash property from the receipt
            Debug.Log("Character created. Tx Hash: " + transactionReceipt.TransactionHash);
        }
        catch (Exception e)
        {
            // Added StackTrace for more detailed error information
            Debug.LogError($"Error creating character: {e.Message}\n{e.StackTrace}");
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
            // Convert the result to JSON for inspection
            var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
            Debug.Log("Full Result:\n" + jsonResult);

            // Deserialize result[0] = characterIds
            var characterIds = ((JArray)JToken.FromObject(result[0])).ToObject<List<ulong>>();
            Debug.Log("Character IDs: " + string.Join(", ", characterIds));

            // Deserialize result[1] = characterData array
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
                // Use .SelectToken("Result") to get the actual value from the ABI wrapper
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
