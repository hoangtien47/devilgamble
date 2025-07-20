using Map;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private CharacterCardDataSet CharacterCardData; // Example variable to hold character card data
    [SerializeField]
    private LevelSO LevelDataSO;

    private LevelData LevelData; // Example variable to hold level data

    private CharacterCardData CharacterCardChosen;

    private CharacterCardData EnemyData;

    public ContractManager _contractManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(this.gameObject); // Ensure only one instance exists
        }

        _contractManager = FindFirstObjectByType<ContractManager>();
        LevelDataSO.LoadLevelsFromPrefs();
    }

   

    #region Level Management
    public List<LevelData> GetLevels()
    {
        return LevelDataSO.GetLevels();
    }
    public int GetLevelCount()
    {
        return LevelDataSO.GetLevels().Count;
    }
    private bool IsSavedLevel()
    {
        return LevelDataSO.IsSavedLevel(LevelData);
    }
    public void SetCurrentLevel(LevelData level)
    {
        this.LevelData = level;
    }
    public bool SetCurrentLevelStatus(bool isDone)
    {
        if(LevelData == null)
        {
            Debug.LogError("Current level data is null. Cannot set level status.");
            return false;
        }
        if (LevelData.IsCompleted(isDone))
        {
            if (LevelDataSO.GetLevels().Count > LevelData.Index &&LevelDataSO.GetLevels()[LevelData.Index].LevelStatus == LevelStatus.Locked)
            {
                LevelDataSO.GetLevels()[LevelData.Index].LevelStatus = LevelStatus.InProgress;
                Debug.Log(LevelDataSO.GetLevels()[LevelData.Index].LevelStatus);
            }
            return IsSavedLevel();
        }
        return false;
    }
    public LevelMode GetCurrentLevelMode()
    {
        if (LevelData != null)
        {
            return LevelData.LevelMode;
        }
        Debug.LogError("Current level data is null. Cannot get level mode.");
        return LevelMode.Easy;
    }
    public List<LevelReward> GetCurrentLevelReward()
    {
        if (LevelData != null && LevelData.LevelRewards != null && LevelData.LevelRewards.Count > 0)
        {
            return LevelData.LevelRewards; // Assuming we want the first reward
        }
        Debug.LogError("Current level data or rewards are null. Cannot get level reward.");
        return null;
    }
    public MapConfig GetMapConfig()
    {
        if (LevelData != null && LevelData.MapConfig != null)
        {
            return LevelData.MapConfig;
        }
        Debug.LogError("Current level data or map config is null. Cannot get map config.");
        return null;
    }
    #endregion

    #region Character Management
    public List<CharacterCardData> GetHeroes()
    {
        if (CharacterCardData == null)
        {
            Debug.LogError("CharacterCardData is null. Cannot get heroes.");
            return new List<CharacterCardData>();
        }
        return CharacterCardData.GetHeroes();
    }
    public void SetEnemy(Rarity type)
    {
        Debug.Log(LevelData.Index);
        var enemies = CharacterCardData.GetEnemies(LevelData.Index, type);
        if (CharacterCardData == null)
        {
            Debug.LogError("CharacterCardData is null. Cannot get enemies.");
            return;
        }

        int randomIndex = Random.Range(0, enemies.Count);
        EnemyData = new CharacterCardData(enemies[randomIndex]);
        Debug.Log(EnemyData.ToString());
    }

    public CharacterCardData GetEnemyData()
    {
        if (EnemyData == null)
        {
            Debug.LogError("EmenyData is null. Cannot get enemy data.");
            return null;
        }
        return EnemyData;
    }

    public CharacterCardData GetCharacterCardChosen()
    {
        if (CharacterCardChosen == null)
        {
            Debug.LogError("CharacterCardChosen is null. Cannot get chosen character card.");
            return null;
        }
        return CharacterCardChosen;
    }
    public void SetCharacterCardChosen(CharacterCardData characterCardData)
    {
        if (characterCardData == null)
        {
            Debug.LogError("Cannot set CharacterCardChosen to null.");
            return;
        }
        CharacterCardChosen = characterCardData;
    }

    public void SetCharacterCardChosen(CharacterModel characterModel)
    {
        if (characterModel == null)
        {
            Debug.LogError("Cannot set CharacterCardChosen to null.");
            return;
        }
        CharacterCardChosen = new CharacterCardData(characterModel);
    }

    #endregion

    #region Scene Management
    public async Task LoadSceneAsync(string sceneName)
    {

        try
        {
            var loadOperation = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            await loadOperation.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene {sceneName}: {e.Message}");
        }
    }
    #endregion

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
    public void OpenLevelScence()
    {
        LoadSceneAsync("LevelScene");
    }
    public void OpenMapScence()
    {
        LoadSceneAsync("MapScene");
    }
    public void OpenBattleScence()
    {
        LoadSceneAsync("Balatro-Feel");
    }
    public void OpenLobby()
    {
        LoadSceneAsync("LobbyScene");
    }
}
