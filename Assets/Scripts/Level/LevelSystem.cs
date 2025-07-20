using Map;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelSystem
{
    private List<LevelData> levels;
    private LevelManager levelManager;
    public event Action<LevelData> OnLevelChanged;
    public int CurrentLevelIndex { get; private set; } = 0;
    public LevelSystem(List<LevelData> levels, LevelManager manager)
    {
        this.levels = levels;
        this.levelManager = manager;
    }

    public void InitLevelView(int defaultIndex = 0)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("No levels available to initialize the level system.");
            return;
        }
        ChangeLevel(defaultIndex);
    }
    public void ChangeLevel(int index)
    {
        if (index < 0 || index >= levels.Count)
        {
            Debug.LogError($"Invalid level index: {index}. Cannot change level.");
            return;
        }
        levelManager.SetCurrentLevel(levels[index]);
        OnLevelChanged?.Invoke(levels[index]);
    }
    public void ChangeLevelMode(LevelMode newMode)
    {
        if(CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Count)
        {
            levels[CurrentLevelIndex].LevelMode = newMode;
            levelManager.SetCurrentLevel(levels[CurrentLevelIndex]);
        }
    }
    public void SetCurrentLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("Level is null. Cannot set current level.");
            return;
        }
        CurrentLevelIndex = levels.IndexOf(level);
        GameManager.Instance.SetCurrentLevel(level);
    }
    public bool LevelIsLocked()
    {
        if(CurrentLevelIndex < 0 || CurrentLevelIndex >= levels.Count)
        {
            Debug.LogError("Current level index is out of bounds. Cannot get current level status.");
            return true;
        }
        LevelData currentLevel = levels[CurrentLevelIndex];
        return currentLevel.IsLocked();
    }
}


