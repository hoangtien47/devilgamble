using System.Numerics;
using UnityEngine;

/// <summary>
/// Simple test script to test character creation with proper permissions
/// </summary>
public class CreateCharacterTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestOnStart = false;
    public bool diagnosePermissionsOnly = true;
    
    [Header("Character Data for Testing")]
    public string characterName = "Fire Knight";
    public string characterDescription = "A brave knight wielding the power of fire magic";
    public Rarity rarity = Rarity.Legendary;
    public CharacterTeam team = CharacterTeam.Hero;
    public int baseAttack = 120;
    public int baseHealth = 150;
    public int baseSpeed = 80;
    public int level = 1;
    public string priceInWei = "5000000000000000"; // 0.005 ETH

    private ContractManagerFixed contractManager;

    private async void Start()
    {
        contractManager = FindFirstObjectByType<ContractManagerFixed>();
        if (contractManager == null)
        {
            Debug.LogError("ContractManagerFixed not found in scene!");
            return;
        }

        if (runTestOnStart)
        {
            if (diagnosePermissionsOnly)
            {
                await TestPermissions();
            }
            else
            {
                await TestCreateCharacter();
            }
        }
    }

    [ContextMenu("Test Permissions")]
    public async System.Threading.Tasks.Task TestPermissions()
    {
        if (contractManager == null)
        {
            Debug.LogError("ContractManagerFixed not found!");
            return;
        }

        Debug.Log("=== TESTING PERMISSIONS ===");
        await contractManager.DiagnosePermissions();
    }

    [ContextMenu("Test Create Character")]
    public async System.Threading.Tasks.Task TestCreateCharacter()
    {
        if (contractManager == null)
        {
            Debug.LogError("ContractManagerFixed not found!");
            return;
        }

        Debug.Log("=== TESTING CHARACTER CREATION ===");

        var testCharacter = new CharacterCardData
        {
            characterName = characterName,
            characterDescription = characterDescription,
            rarity = rarity,
            characterTeam = team,
            baseAttack = baseAttack,
            baseHealth = baseHealth,
            baseSpeed = baseSpeed,
            level = level,
            price = BigInteger.Parse(priceInWei),
            
            // Set current stats same as base stats for new character
            currentAttack = baseAttack,
            currentHealth = baseHealth,
            currentSpeed = baseSpeed
        };

        await contractManager.CreateCharacter(testCharacter);
    }

    [ContextMenu("Set Game Manager")]
    public async void SetGameManager()
    {
        if (contractManager == null)
        {
            Debug.LogError("ContractManagerFixed not found!");
            return;
        }

        Debug.Log("=== SETTING GAME MANAGER ===");
        
        // This will set the current wallet as game manager
        // You need to provide the address you want to set as game manager
        string addressToSetAsGameManager = "0xf44127238ab620fb5478063120ecc0aef1cdfcd0"; // Current wallet address
        
        bool success = await contractManager.SetGameManager(addressToSetAsGameManager);
        
        if (success)
        {
            Debug.Log("✅ Game manager set successfully!");
        }
        else
        {
            Debug.LogError("❌ Failed to set game manager!");
        }
    }

    private void OnGUI()
    {
        if (contractManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Create Character Tester", GUI.skin.box);
        
        if (GUILayout.Button("Diagnose Permissions"))
        {
            TestPermissions();
        }
        
        if (GUILayout.Button("Test Create Character"))
        {
            TestCreateCharacter();
        }
        
        if (GUILayout.Button("Set Game Manager"))
        {
            SetGameManager();
        }
        
        GUILayout.EndArea();
    }
}
