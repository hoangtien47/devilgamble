using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;
    public static GameStateManager Instance => _instance;

    // Constants for PlayerPrefs keys
    private const string BEST_DAMAGE_DEALT_KEY = "BestDamageDealt";
    private const string DAMAGE_TAKEN_KEY = "DamageTaken";

    // Scene index constants (adjust these based on your actual scene indices)
    private const int COMBAT_SCENE_INDEX = 3;

    // Current battle statistics
    private int currentDamageDealt = 0;
    private int currentDamageTaken = 0;

    // Game state properties
    public bool IsVictory { get; private set; }

    public int BestDamageDealt => PlayerPrefs.GetInt(BEST_DAMAGE_DEALT_KEY, 0);
    public int BestDamageTaken => PlayerPrefs.GetInt(DAMAGE_TAKEN_KEY, 0);
    public int CurrentDamageDealt => currentDamageDealt;
    public int CurrentDamageTaken => currentDamageTaken;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize game state
        IsVictory = false;
    }

    public void ClearAllData()
    {
        // Clear current battle statistics
        ResetBattleStats();

        // Reset any other relevant data
        currentDamageDealt = 0;
        currentDamageTaken = 0;
        PlayerPrefs.SetInt(BEST_DAMAGE_DEALT_KEY, 0);
        PlayerPrefs.SetInt(DAMAGE_TAKEN_KEY, 0);
        PlayerPrefs.Save();

        // Reset game state
        IsVictory = false;

        Debug.Log("GameState data cleared for new combat");
    }

    public void TrackDamageDealt(int damage)
    {
        currentDamageDealt = damage;
        if (currentDamageDealt > BestDamageDealt)
        {
            PlayerPrefs.SetInt(BEST_DAMAGE_DEALT_KEY, currentDamageDealt);
            PlayerPrefs.Save();
        }
    }

    public void TrackDamageTaken(int damage)
    {
        currentDamageTaken += damage;
        PlayerPrefs.SetInt(DAMAGE_TAKEN_KEY, currentDamageTaken);
        PlayerPrefs.Save();
    }

    public void OnBattleWin()
    {
        Debug.Log($"Battle Won! Damage Dealt: {currentDamageDealt}, Damage Taken: {currentDamageTaken}");
        IsVictory = true;
        SendGameResultToReact(true); // Send win result to React
        ShowGameOverUI(true);
    }

    public void OnBattleLose()
    {
        Debug.Log($"Battle Lost! Damage Dealt: {currentDamageDealt}, Damage Taken: {currentDamageTaken}");
        IsVictory = false;
        SendGameResultToReact(false); // Send lose result to React
        ShowGameOverUI(false);
    }

    // This method sends the game result to React frontend
    private void SendGameResultToReact(bool isVictory)
    {
        // Create result object with all the info React might need
        GameResult result = new GameResult
        {
            isVictory = isVictory,
            damageDealt = currentDamageDealt,
            damageTaken = currentDamageTaken,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Convert result to JSON
        string resultJson = JsonConvert.SerializeObject(result);

        // Call JavaScript function to send data to React
#if UNITY_WEBGL && !UNITY_EDITOR
            SendResultToReactJS(resultJson);
#else
        Debug.Log($"Would send to React: {resultJson}");
#endif
    }

    private void ShowGameOverUI(bool isWin)
    {
        // Find and activate the GameOverUI
        var gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI != null)
        {
            gameOverUI.Show(isWin, currentDamageDealt, currentDamageTaken);
        }
        ResetBattleStats();
    }

    public void ResetBattleStats()
    {
        currentDamageDealt = 0;
        currentDamageTaken = 0;
    }

    [DllImport("__Internal")]
    private static extern void SendResultToReactJS(string resultJson);
}

// Class to hold the game result data for serialization
[Serializable]
public class GameResult
{
    public bool isVictory;
    public int damageDealt;
    public int damageTaken;
    public long timestamp;
}