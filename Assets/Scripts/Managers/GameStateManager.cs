using UnityEngine;
using UnityEngine.SceneManagement;

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
        ShowGameOverUI(true);
    }

    public void OnBattleLose()
    {
        Debug.Log($"Battle Lost! Damage Dealt: {currentDamageDealt}, Damage Taken: {currentDamageTaken}");
        ShowGameOverUI(false);
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
}