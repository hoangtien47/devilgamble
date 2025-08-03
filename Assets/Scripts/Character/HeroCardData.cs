using UnityEngine;

/// <summary>
/// Data class to store hero character information
/// </summary>
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

    // Default constructor
    public HeroCardData()
    {
        id = "";
        Name = "";
        Description = "";
        Sprite = null;
        maxHealth = 100;
        currentHealth = 100;
        attack = 10;
        defense = 0;
    }

    // Constructor from CharacterCardData
    public HeroCardData(CharacterCardData source)
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
    }

    // Method to copy data from another HeroCardData
    public void SetData(HeroCardData other)
    {
        if (other == null) return;

        this.id = other.id;
        this.Name = other.Name;
        this.Description = other.Description;
        this.Sprite = other.Sprite;
        this.maxHealth = other.maxHealth;
        this.currentHealth = other.currentHealth;
        this.attack = other.attack;
        this.defense = other.defense;
    }
}
