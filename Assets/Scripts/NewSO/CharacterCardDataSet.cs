using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "New Card Data Set", menuName = "Card Game/Character Card Data Set")]
public class CharacterCardDataSet : ScriptableObject
{
    [Header("CSV Data")]
    [SerializeField, Tooltip("Comma Separated Values that can be used to create the character cards. Organized as \"Name, Description, SpriteAddress, FragmentIconAddress, BaseAttack, BaseHealth, BaseTurnToAttack, MaxUpgradeLevel, BaseFragmentDropRate, UpgradeLevel1_FragmentsRequired, UpgradeLevel1_AttackBonus, UpgradeLevel1_HealthBonus, ...\"")]
    private TextAsset _characterCSV;

    [Header("Generated Characters")]
    [SerializeField]
    private List<CharacterCardData> _characterCardsList = new List<CharacterCardData>();

    // Remove the Dictionary completely - use List only
    public List<CharacterCardData> CharacterCards => _characterCardsList;

    [ContextMenu("Create Character Cards from CSV")]
    public async void CreateCharacterCardsFromCSV()
    {
        _characterCardsList.Clear();

        if (_characterCSV == null)
        {
            Debug.LogWarning("No CSV Assigned.");
            return;
        }

        string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        char[] TRIM_CHARS = { '\"' };

        List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

        TextAsset data = _characterCSV;
        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length > 1)
        {
            var header = Regex.Split(lines[0], SPLIT_RE);
            for (var i = 1; i < lines.Length; i++)
            {
                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || values[0] == "") continue;

                var entry = new Dictionary<string, object>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j];
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                    value = value.Replace("\"\"", "\"");

                    object finalvalue = value;
                    entry[header[j]] = finalvalue;
                }
                list.Add(entry);
            }
        }

        // Process each character data
        for (int i = 0; i < list.Count; i++)
        {
            CharacterCardData character = await ProcessCharacterDataAsync(list[i]);
            if (character != null)
            {
                _characterCardsList.Add(character);
            }
        }

        Debug.Log($"Successfully loaded {_characterCardsList.Count} characters from CSV");
    }

    private async System.Threading.Tasks.Task<CharacterCardData> ProcessCharacterDataAsync(Dictionary<string, object> characterData)
    {
        CharacterCardData character = new CharacterCardData();

        // Set basic character info
        character.characterName = characterData["Name"] as string;
        character.characterDescription = characterData["Description"] as string;

        // Parse character team
        string teamString = characterData["CharacterTeam"] as string;
        if (System.Enum.TryParse<CharacterTeam>(teamString, true, out CharacterTeam team))
        {
            character.characterTeam = team;
        }
        else
        {
            Debug.LogWarning($"Invalid team '{teamString}' for character '{character.characterName}'. Using default (Hero).");
            character.characterTeam = CharacterTeam.Hero;
        }

        string rarityString = characterData["Rarity"] as string;
        if (System.Enum.TryParse<Rarity>(rarityString, true, out Rarity rarity))
        {
            character.rarity = rarity;
        }
        else
        {
            Debug.LogWarning($"Invalid rarity '{rarityString}' for character '{character.characterName}'. Using default (Hero).");
            character.rarity = Rarity.Rare;
        }

        // Load sprites from Addressables using the address paths
        string spriteAddress = characterData["SpriteAddress"] as string;
        if (!string.IsNullOrEmpty(spriteAddress))
        {
            try
            {
                AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(spriteAddress);
                character.characterSprite = await spriteHandle.Task;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load sprite '{spriteAddress}': {e.Message}");
            }
        }

        if (int.TryParse(characterData["BaseAttack"] as string, out int attack))
            character.baseAttack = attack;

        if (int.TryParse(characterData["BaseHealth"] as string, out int health))
            character.baseHealth = health;

        if (int.TryParse(characterData["Speed"] as string, out int speed))
            character.baseSpeed = speed;

        if (int.TryParse(characterData["Level"] as string, out int Level))
            character.level = Level;

        if (int.TryParse(characterData["Price"] as string, out int price))
            character.price = price;

        return character;
    }

    public CharacterCardData GetCharacterByName(string name)
    {
        return _characterCardsList.FirstOrDefault(character => character.characterName == name);
    }

    public bool HasCharacter(string name)
    {
        return _characterCardsList.Any(character => character.characterName == name);
    }

    public string[] GetAllCharacterNames()
    {
        return _characterCardsList.Select(character => character.characterName).ToArray();
    }

    public List<CharacterCardData> GetCharactersByTeam(CharacterTeam team)
    {
        return _characterCardsList.Where(character => character.characterTeam == team).ToList();
    }

    public List<CharacterCardData> GetHeroes()
    {
        return GetCharactersByTeam(CharacterTeam.Hero);
    }

    public List<CharacterCardData> GetEnemies(int level, Rarity type)
    {
        Debug.Log("TEST");
        return GetCharactersByTeam(CharacterTeam.Enemy).Where(character => character.level == level && character.rarity == type).ToList();
    }
}

[System.Serializable]
public class CharacterCardData
{
    [Header("Base Card Info")]
    public string characterName;
    public string characterDescription;
    public Sprite characterSprite;
    public Rarity rarity = Rarity.Common; // Default rarity
    public CharacterTeam characterTeam = CharacterTeam.Hero;
    public int baseAttack;
    public int baseHealth;
    public int baseSpeed;
    public int currentHealth;
    public int currentAttack;
    public int currentSpeed;
    public int level;
    public BigInteger price;

    public CharacterCardData() { }

    public CharacterCardData(CharacterCardData characterCardData)
    {
        characterName = characterCardData.characterName;
        characterDescription = characterCardData.characterDescription;
        characterSprite = characterCardData.characterSprite;
        rarity = characterCardData.rarity;
        characterTeam = characterCardData.characterTeam;
        baseAttack = characterCardData.baseAttack;
        baseHealth = characterCardData.baseHealth;
        baseSpeed = characterCardData.baseSpeed;
        currentHealth = characterCardData.baseHealth;
        currentAttack = characterCardData.baseAttack;
        currentSpeed = characterCardData.baseSpeed;
        level = characterCardData.level;
        price = characterCardData.price;
    }

    public CharacterCardData(CharacterModel characterModel)
    {
        characterName = characterModel.CharacterName;
        characterDescription = characterModel.CharacterDescription;
        characterSprite = characterModel.CharacterSprite;
        rarity = characterModel.Rarity;
        characterTeam = characterModel.Team;
        baseAttack = characterModel.BaseAttack;
        baseHealth = characterModel.BaseHealth;
        baseSpeed = characterModel.BaseSpeed;
        currentHealth = characterModel.CurrentHealth; // Initialize current health to base health
        currentAttack = characterModel.CurrentAttack; // Initialize current attack to base attack
        currentSpeed = characterModel.CurrentSpeed; // Initialize current speed to base speed
        level = characterModel.Level;
        price = characterModel.Price;
    }

    public override string ToString()
    {
        return $"{characterName} - {characterDescription} - {currentHealth} - {currentAttack} (Team: {characterTeam}, Rarity: {rarity}, Level: {level}, Price: {price})";
    }

}

[System.Serializable]
public enum CharacterTeam
{
    Hero,
    Enemy,
    Neutral
}


[System.Serializable]
public enum Rarity
{
    Minor,
    Elite,
    Boss,
    Common,
    Rare,
    Epic,
    Legendary
}

