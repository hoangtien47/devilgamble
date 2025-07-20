using Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "LevelSO", menuName = "Scriptable Objects/LevelSO")]
public class LevelSO : ScriptableObject
{
    private const string LEVEL_KEY = "PLAYER_LEVELDATA";

    [Header("CSV Data")]
    [SerializeField, Tooltip("Comma Separated Values that can be used to create the Level. Organized as \"UUID, Name, MapConfig, LevelStatus, SpriteKey\"")]
    private TextAsset _LevelCSV;

    [Header("Generated Levels")]
    [SerializeField]
    private List<LevelData> _Levels;
    [SerializeField]
    private List<LevelData> _LevelsDefaults;

    public List<LevelData> GetLevels() => _Levels;
    public int GetLevelsCount() => _Levels.Count;
    [ContextMenu("Create LevelData from CSV")]
    public async void CreateLevelFromCSV()
    {
        _LevelsDefaults.Clear();

        if (_LevelCSV == null)
        {
            Debug.LogWarning("No CSV Assigned.");
            return;
        }

        string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        char[] TRIM_CHARS = { '\"' };

        List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

        TextAsset data = _LevelCSV;
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
            LevelData levelData = await ProcessLevelDataAsync(list[i]);
            if (levelData != null)
            {
                _LevelsDefaults.Add(levelData);
            }
        }
        LoadLevelsFromPrefs();
        Debug.Log($"Successfully loaded {_Levels.Count} Level from CSV");
    }

    private async Task<LevelData> ProcessLevelDataAsync(Dictionary<string, object> levelCSVData)
    {
        LevelData levelData = new LevelData();
        // UUIDLevel is mandatory, so we check if it exists and is a string
        if (levelCSVData.TryGetValue("Index", out object indexObj) &&
            indexObj is string indexStr &&
            int.TryParse(indexStr, out int index))
        {
            levelData.Index = index;
        }
        else
        {
            Debug.LogWarning("Index not found or invalid in CSV data.");
            return null;
        }
        // LevelName is mandatory, so we check if it exists and is a string
        if (levelCSVData.TryGetValue("LevelName", out object nameObj) && nameObj is string name)
        {
            levelData.LevelName = name;
        }
        else
        {
            Debug.LogWarning("LevelName not found in CSV data.");
            return null;
        }
        // LevelDescription is optional, so we check if it exists
        if (levelCSVData.TryGetValue("LevelDescription", out object levelDescriptionObj) && levelDescriptionObj is string levelDescription)
        {
            levelData.LevelDescription = levelDescription;
        }
        else
        {
            Debug.LogWarning("LevelDescription not found in CSV data.");
            return null;
        }
        // MapConfigKey is mandatory, so we check if it exists and is a string
        if (levelCSVData.TryGetValue("MapConfigKey", out object mapConfigObj) && mapConfigObj is string mapConfigKey)
        {
            levelData.MapConfig = await LoadMapConfigAsync(mapConfigKey);
        }
        else
        {
            Debug.LogWarning("MapConfigKey not found in CSV data.");
            return null;
        }
        // LevelStatus is optional, so we check if it exists and is a valid enum value
        if (levelCSVData.TryGetValue("LevelStatus", out object levelStatusObj) && levelStatusObj is string levelStatusStr)
        {
            if (Enum.TryParse(levelStatusStr, out LevelStatus levelStatus))
            {
                levelData.LevelStatus = levelStatus;
            }
            else
            {
                Debug.LogWarning($"Invalid LevelStatus '{levelStatusStr}' in CSV data. Defaulting to Locked.");
                levelData.LevelStatus = LevelStatus.Locked;
            }
        }
        else
        {
            Debug.LogWarning("LevelStatus not found in CSV data. Defaulting to Locked.");
            levelData.LevelStatus = LevelStatus.Locked;
        }
        // MaxPoints is optional, so we check if it exists and is a valid integer
        if (levelData.LevelStatus != LevelStatus.Locked)
        {
            if (levelCSVData.TryGetValue("MaxPoints", out object maxPointsObj) && maxPointsObj is string maxPointsStr && int.TryParse(maxPointsStr, out int maxPoints))
            {
                levelData.MaxPoints = maxPoints;
            }
            else
            {
                Debug.LogWarning("MaxPoints not found or invalid in CSV data. Defaulting to 0.");
                levelData.MaxPoints = 0;
            }
        }
        else
        {
            levelData.MaxPoints = 0; // Locked levels have no points
        }
        // SpriteKey is optional, so we check if it exists and is a string
        if (levelCSVData.TryGetValue("LevelSpriteKey", out object spriteKeyObj) && spriteKeyObj is string spriteKey)
        {
            levelData.LevelSprite = await LoadLevelSpriteAsync(spriteKey);
        }
        else
        {
            Debug.LogWarning("SpriteKey not found in CSV data.");
            return null;
        }
        // LevelRewards is optional, so we check if it exists and is a string
        if (levelCSVData.TryGetValue("Rewards", out object rewardsObj) && rewardsObj is string rewardsString)
        {
            levelData.LevelRewards = await ParseRewards(rewardsString);
        }
        else
        {
            Debug.LogWarning("LevelRewards not found in CSV data. Defaulting to empty list.");
            levelData.LevelRewards = new List<LevelReward>();
        }
        return levelData;
    }

    private async Task<Sprite> LoadLevelSpriteAsync(string spriteKey)
    {
        if (string.IsNullOrEmpty(spriteKey))
        {
            Debug.LogWarning("SpriteKey is null or empty.");
            return null;
        }
        try
        {
            AsyncOperationHandle<Sprite> sprite = Addressables.LoadAssetAsync<Sprite>(spriteKey);
            return await sprite.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Sprite with name {spriteKey}: {e.Message}");
            return null;
        }
    }

    private async Task<MapConfig> LoadMapConfigAsync(string mapConfigName)
    {
        if (string.IsNullOrEmpty(mapConfigName))
        {
            Debug.LogWarning("MapConfigName is null or empty.");
            return null;
        }
        try
        {
            AsyncOperationHandle<MapConfig> mapConfig = Addressables.LoadAssetAsync<MapConfig>(mapConfigName);
            return await mapConfig.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load MapConfig with name {mapConfigName}: {e.Message}");
            return null;
        }
    }
    private async Task<List<LevelReward>> ParseRewards(string rewardsString)
    {
        List<LevelReward> rewards = new List<LevelReward>();
        if (string.IsNullOrEmpty(rewardsString)) return rewards;
        string[] rewardEntries = rewardsString.Split(';');
        foreach (var entry in rewardEntries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue; // Invalid entry, skip
            string key = parts[0].Trim().ToLower();
            string amount = parts[1].Trim();
            LevelReward reward = new LevelReward();
            switch (key)
            {
                case "gold":
                    reward.RewardType = RewardType.Gold;
                    break;
                case "gems":
                    reward.RewardType = RewardType.Gems;
                    break;
                default:
                    reward.RewardType = RewardType.Item;
                    break;
            }
            reward.RewardID = key; // Use the key as the RewardID
            reward.RewardSprite = await LoadLevelSpriteAsync(key); // Assuming the sprite is named the same as the key
            reward.Amount = int.TryParse(amount, out int parsedAmount) ? parsedAmount : 0;
            rewards.Add(reward);
        }
        return rewards;
    }
    #region Save and Load Levels
    public bool IsSavedLevel(LevelData level)
    {
        try
        {
            if (level == null)
            {
                Debug.LogError("Level data is null. Cannot check if level is saved.");
                return false;
            }
            _Levels[level.Index-1] = level;
            SaveLevels();
            return true;
        }
        catch
        {
            Debug.LogError("Error checking if level is saved."); return false;
        }
    }
    public void SaveLevels()
    {
        try
        {
            var serializableLevels = _Levels.Select(level => new SerializableLevelData(level)).ToList();
            string json = JsonUtility.ToJson(new SerializableLevelList { levels = serializableLevels });
            PlayerPrefs.SetString(LEVEL_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"Successfully saved {_Levels.Count} levels to PlayerPrefs");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save levels: {e.Message}");
        }
    }

    public void LoadLevelsFromPrefs()
    {
        try
        {
            if (!PlayerPrefs.HasKey(LEVEL_KEY))
            {
                Debug.Log("No saved level data found in PlayerPrefs");
                _Levels = new List<LevelData>(_LevelsDefaults); // Initialize with defaults if no saved data
                return;
            }
            _Levels.Clear();
            string json = PlayerPrefs.GetString(LEVEL_KEY);
            Debug.Log(json);
            var savedData = JsonUtility.FromJson<SerializableLevelList>(json);

            if (savedData?.levels == null)
            {
                Debug.LogError("Failed to deserialize level data");
                return;
            }

            // Update existing levels with saved data
            foreach (var existingLevel in _LevelsDefaults)
            {
                var savedLevel = savedData.levels
                    .FirstOrDefault(l => l.Index == existingLevel.Index);
                if (savedLevel != null)
                {
                    // Update only serializable properties
                    existingLevel.LevelStatus = (LevelStatus)savedLevel.LevelStatus;
                    existingLevel.LevelMode = savedLevel.LevelMode;
                    existingLevel.MaxPoints = savedLevel.MaxPoints;

                    // Update rewards if needed
                    if (savedLevel.LevelRewards != null)
                    {
                        foreach (var savedReward in savedLevel.LevelRewards)
                        {
                            var existingReward = existingLevel.LevelRewards?
                                .FirstOrDefault(r => r.RewardID == savedReward.RewardID);
                            if (existingReward != null)
                            {
                                existingReward.Amount = savedReward.Amount;
                                existingReward.RewardType = savedReward.RewardType;
                            }
                        }
                    }
                }
                _Levels.Add(existingLevel);
            }

            Debug.Log($"Successfully loaded {savedData.levels.Count} levels from PlayerPrefs");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load levels: {e.Message}");
        }
    }

    // Helper class for JSON serialization
    [Serializable]
    private class SerializableLevelList
    {
        public List<SerializableLevelData> levels;
    }
    #endregion
}
#region LevelData Extensions
[Serializable]
public class LevelData
{
    [Header("Level Data")]
    public int Index;
    public string LevelName;
    public string LevelDescription;
    public int MaxPoints = 0;
    public MapConfig MapConfig;
    public LevelMode LevelMode = LevelMode.Easy;
    public LevelStatus LevelStatus = LevelStatus.Locked;
    public List<LevelReward> LevelRewards;
    [Header("Level Sprite")]
    public Sprite LevelSprite;
    
    public LevelMode GetNextMode()
    {
        return LevelMode switch
        {
            LevelMode.Easy => LevelMode.Normal,
            LevelMode.Normal => LevelMode.Hard,
            LevelMode.Hard => LevelMode.Expert,
            LevelMode.Expert => LevelMode.Hell,
            LevelMode.Hell => LevelMode.Easy,
            _ => LevelMode.Normal
        };
    }

    public LevelMode GetPreviousMode()
    {
        return LevelMode switch
        {
            LevelMode.Easy => LevelMode.Hell,
            LevelMode.Normal => LevelMode.Easy,
            LevelMode.Hard => LevelMode.Normal,
            LevelMode.Expert => LevelMode.Hard,
            LevelMode.Hell => LevelMode.Expert,
            _ => LevelMode.Normal
        };
    }
    public bool IsCompleted(bool isCompleted)
    {
        switch (LevelStatus)
        {
            case LevelStatus.Locked:
                return false;
            case LevelStatus.Completed:
                return isCompleted;
            case LevelStatus.InProgress:
                LevelStatus = isCompleted? LevelStatus.Completed: LevelStatus.InProgress;
                return isCompleted;
            default:
                return false;
        }
    }
    public bool IsLocked()
    {
        return LevelStatus == LevelStatus.Locked;
    }
}
[Serializable]
public class  LevelReward
{
    public RewardType RewardType;
    public string RewardID;
    public Sprite RewardSprite;
    public int Amount = 0;
}
public enum RewardType
{
    Gold,
    Gems,
    Item,
}
public enum LevelStatus
{
    Locked,
    Completed,
    InProgress,
}

public enum LevelMode
{
    Easy,
    Normal,
    Hard,
    Expert,
    Hell,
}
#endregion

#region Serlializable Classes For Save Load Data
[Serializable]
public class SerializableLevelData
{
    public int Index;
    public string LevelName;
    public string LevelDescription;
    public int MaxPoints;
    public LevelMode LevelMode;
    public LevelStatus LevelStatus;
    public List<SerializableLevelReward> LevelRewards;

    // Convert from LevelData
    public SerializableLevelData(LevelData level)
    {
        Index = level.Index;
        LevelName = level.LevelName;
        LevelDescription = level.LevelDescription;
        MaxPoints = level.MaxPoints;
        LevelMode = level.LevelMode;
        LevelStatus = level.LevelStatus;
        LevelRewards = level.LevelRewards?.Select(r => new SerializableLevelReward(r)).ToList()
                      ?? new List<SerializableLevelReward>();
    }
}

[Serializable]
public class SerializableLevelReward
{
    public RewardType RewardType;
    public string RewardID;
    public int Amount;

    public SerializableLevelReward(LevelReward reward)
    {
        RewardType = reward.RewardType;
        RewardID = reward.RewardID;
        Amount = reward.Amount;
    }
}
#endregion