using Map;
using UnityEngine;

public static class GameSession
{
    public static NodeBlueprint node;
    public static HeroCardData heroes;  // Changed from HeroCardScriptable to a new data class
    public static EnemyCardData enemies; // Changed from EnemyCardScriptable to a new data class
    public static int levelEnemy = 1; // Default level for enemies
}

// New class to store hero data separately from ScriptableObject
[System.Serializable]
public class HeroCardData
{
    public string id;
    public string Name;
    public string Description;
    public Sprite Sprite;
    public int maxHealth;
    public int currentHealth;
    public int attack;
    public int defense;
    public bool isUnlocked = true; // Default to true for the active hero

    // Constructor to copy data from ScriptableObject
    public HeroCardData(HeroCardScriptable source)
    {
        if (source == null) return;

        this.id = source.id;
        this.Name = source.Name;
        this.Description = source.Description;
        this.Sprite = source.Sprite;
        this.maxHealth = source.maxHealth;
        this.currentHealth = source.maxHealth; // Start with full health
        this.attack = source.attack;
        this.defense = source.defense;
    }
    public void SetData(HeroesCharacter hero)
    {
        this.currentHealth = hero.HP;
        this.attack = hero.ATK;
        this.Name = hero.Name;
        this.Sprite = hero.Sprite;
    }
    public void HealForRest()
    {
        // Calculate lost HP
        int lostHP = maxHealth - currentHealth;

        // Calculate 30% of lost HP
        int healAmount = Mathf.RoundToInt(lostHP * 0.3f);

        // Add healing but don't exceed max health
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
    }
}
// New class to store enemy data separately from ScriptableObject
[System.Serializable]
public class EnemyCardData
{
    public string id;
    public string Name;
    public string Description;
    public Sprite Sprite;
    public int maxHealth;
    public int currentHealth;
    public int attack;
    public int defense;
    public int actionTurns = 2; // Number of turns before the enemy attacks

    // Constructor to copy data from ScriptableObject with level-based scaling
    public EnemyCardData(EnemyCardScriptable source)
    {
        if (source == null) return;

        this.id = source.id;
        this.Name = source.Name;
        this.Description = source.Description;
        this.Sprite = source.Sprite;

        // Calculate scaled health based on formula: base * (1 + 0.5 * level)
        float healthMultiplier = 1f + (0.2f * (GameSession.levelEnemy-1));
        this.maxHealth = Mathf.RoundToInt(source.maxHealth * healthMultiplier);
        this.currentHealth = this.maxHealth; // Start with full health

        // You might want to scale attack and defense similarly
        float statMultiplier = 1f + (0.2f * (GameSession.levelEnemy - 1));
        this.attack = Mathf.RoundToInt(source.attack * statMultiplier);
        this.actionTurns = source.actionTurns;
        GameSession.levelEnemy+= 1; // Increment level for next enemy
    }
}