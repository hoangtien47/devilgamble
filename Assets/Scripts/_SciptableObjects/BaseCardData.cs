using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Card Game/Base Card Data")]
public class BaseCardData : ScriptableObject
{
    [Header("Base Card Info")]
    public string cardName;
    public string description;
    public Sprite cardArt;
    public CardRarity rarity;
    public int baseCost;
    public int baseAttack;
    public int baseHealth;

    [Header("Upgrade Info")]
    public int maxUpgradeLevel = 5;
    public UpgradeData[] upgradeData;

    [Header("Fragment Info")]
    public Sprite fragmentIcon;
    public int baseFragmentDropRate = 1; // How many fragments drop from defeating this enemy
}

[System.Serializable]
public class UpgradeData
{
    public int level;
    public int fragmentsRequired; // Fragments needed for this upgrade
    public int attackBonus;
    public int healthBonus;
    public int costReduction;
    public string upgradeDescription;
}

public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}