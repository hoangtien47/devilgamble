using Map;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class LevelManager : MonoBehaviour
{
    //This is the config for the levels in the game if player plays the game for the first time
    [Header("UI TextMesh")]
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI levelModeText;

    [Header("UI Image")]
    public Image LevelImage;

    [Header("UI Button")]
    public Button PreLevelButton;
    public Button NextLevelButton;
    public Button PreModeButton;
    public Button NextModeButton;
    public Button LoadMapButton;

    [Header("UI Prefabs")]
    public GameObject ItemPrefab;

    [Header("UI Content")]
    public Transform ItemContent;

    private LevelSystem levelSystem;
    private void Awake()
    {
        InitLevelSystem();

        PreLevelButton.onClick.AddListener(OnPreLevelButtonClicked);
        NextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);

        PreModeButton.onClick.AddListener(OnPreModeButtonClicked);
        NextModeButton.onClick.AddListener(OnNextModeButtonClicked);

        LoadMapButton.onClick.AddListener(LoadMap);
    }
    private void Start()
    {
        
    }
    public void InitLevelSystem()
    {
        var allLevels = GameManager.Instance.GetLevels();
        List<LevelData> levelModels = new List<LevelData>();
        for (int i = 0; i < allLevels.Count; i++)
        {
            LevelData level = allLevels[i];
            levelModels.Add(level);
        }
        levelSystem = new LevelSystem(levelModels, this);
        //Remember to set the default level index, here we assume 0 is the first level
        levelSystem.InitLevelView();
    }

    public void SetCurrentLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null. Cannot set current level.");
            return;
        }
        // Update UI with the current level data
        levelNameText.text = levelData.LevelName;
        LevelImage.sprite = levelData.LevelSprite;
        levelModeText.text = levelData.LevelMode.ToString(); // Assuming LevelMode is an enum or string
        SetUpItems(levelData.LevelRewards);
        levelSystem.SetCurrentLevel(levelData);
        CheckStatusMap(); // Check if the map is locked or not
    }

    private void SetUpItems(List<LevelReward> rewards)
    {
        foreach (Transform child in ItemContent)
        {
            Destroy(child.gameObject); // Clear previous items. Maybe in the future we can use a pool system to optimize this.
        }
        foreach (LevelReward reward in rewards)
        {
            GameObject item = Instantiate(ItemPrefab, ItemContent);
            // Assuming ItemPrefab has a script to set up the item details
            ItemUi itemDetails = item.GetComponent<ItemUi>();
            if (itemDetails != null)
            {
                itemDetails.SetUpItem(reward);
            }
        }
    }
    #region UI Buttons Actions
    private void OnPreLevelButtonClicked()
    {
        if (levelSystem != null && levelSystem.CurrentLevelIndex > 0)
        {
            levelSystem.ChangeLevel(levelSystem.CurrentLevelIndex - 1);
        }
    }

    private void OnNextLevelButtonClicked()
    {
        if (levelSystem != null && levelSystem.CurrentLevelIndex < GameManager.Instance.GetLevelCount() - 1)
        {
            levelSystem.ChangeLevel(levelSystem.CurrentLevelIndex + 1);
        }
    }
    private void OnPreModeButtonClicked()
    {
        if (levelSystem != null && GameManager.Instance.GetLevelCount() > 0)
        {
            var currentLevel = GameManager.Instance.GetLevels()[levelSystem.CurrentLevelIndex];
            var newMode = currentLevel.GetPreviousMode();
            levelSystem.ChangeLevelMode(newMode);
        }
    }

    private void OnNextModeButtonClicked()
    {
        if (levelSystem != null && GameManager.Instance.GetLevelCount() > 0)
        {
            var currentLevel = GameManager.Instance.GetLevels()[levelSystem.CurrentLevelIndex];
            var newMode = currentLevel.GetNextMode();
            levelSystem.ChangeLevelMode(newMode);
        }
    }
    #endregion
    #region LoadScene
    private void CheckStatusMap()
    {
        if (levelSystem.LevelIsLocked())
        {
            LoadMapButton.GetComponent<CanvasGroup>().interactable = false;
        }
        else
        {
            LoadMapButton.GetComponent<CanvasGroup>().interactable = true;
        }
    }

    public void LoadMap()
    {
        _ = LoadSceneAsync("ChoseHeroesScene");
    }

    private async Task LoadSceneAsync(string sceneName)
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
}

public class Level
{
    public int LevelIndex;
    public MapConfig MapConfig;

    public GameObject LevelView;

    public LevelStatus Status = LevelStatus.Locked;
}
