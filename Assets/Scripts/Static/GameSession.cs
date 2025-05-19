using Map;
using UnityEngine;

public static class GameSession
{
    public static NodeBlueprint node;
    public static HeroCardData heroes;  // Changed from HeroCardScriptable to a new data class
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
}
