using UnityEngine;

/// <summary>
/// Data class to store enemy character information
/// </summary>
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

    // Default constructor
    public EnemyCardData()
    {
        id = "";
        Name = "";
        Description = "";
        Sprite = null;
        maxHealth = 100;
        currentHealth = 100;
        attack = 10;
        defense = 0;
        actionTurns = 2;
    }

    // Constructor to copy data from ScriptableObject with level-based scaling
    public EnemyCardData(EnemyCardScriptable source, int level = 1)
    {
        if (source == null) return;

        this.id = source.id;
        this.Name = source.Name;
        this.Description = source.Description;
        this.Sprite = source.Sprite;

        // Calculate scaled health based on formula: base * (1 + 0.2 * (level-1))
        float healthMultiplier = 1f + (0.2f * (level - 1));
        this.maxHealth = Mathf.RoundToInt(source.maxHealth * healthMultiplier);
        this.currentHealth = this.maxHealth; // Start with full health

        // Scale attack similarly
        float attackMultiplier = 1f + (0.15f * (level - 1));
        this.attack = Mathf.RoundToInt(source.attack * attackMultiplier);

        this.defense = source.defense;
        this.actionTurns = source.actionTurns;
    }

    // Constructor from CharacterCardData
    public EnemyCardData(CharacterCardData source)
    {
        if (source == null) return;

        this.id = source.characterName;
        this.Name = source.characterName;
        this.Description = source.characterDescription;
        this.Sprite = source.characterSprite;
        this.maxHealth = source.baseHealth;
        this.currentHealth = source.currentHealth;
        this.attack = source.baseAttack;
        this.defense = 0;
        this.actionTurns = 2;
    }
}
