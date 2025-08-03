using UnityEngine;

/// <summary>
/// Stub class for GameSession to fix missing references
/// This should be replaced with the actual GameSession implementation
/// </summary>
public static class GameSession
{
    public static HeroCardData heroes;
    public static EnemyCardData enemies;
    
    // Add other static properties as needed
    public static int currentLevel = 1;
    public static int playerGold = 0;
    
    public static void Initialize()
    {
        Debug.Log("GameSession initialized");
    }
    
    public static void SaveHeroData(HeroCardData heroData)
    {
        heroes = heroData;
        Debug.Log("Hero data saved to GameSession");
    }
    
    public static void SaveEnemyData(EnemyCardData enemyData)
    {
        enemies = enemyData;
        Debug.Log("Enemy data saved to GameSession");
    }
}
