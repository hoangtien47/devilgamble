using System;
using System.Numerics;
using UnityEngine;

public class CharacterModel : MonoBehaviour, ICharacter
{
    [Header("Runtime Data - Saved to JSON")]
    private int currentHealth;
    private int baseHealth;
    private int currentAttack;
    private int baseAttack;
    private bool isAlive = true;
    private int baseSpeed;
    private int currentSpeed;
    private int level = 1;
    private Rarity rarity;
    private BigInteger price = 0;

    // Reference to the character data from the dataset
    private CharacterCardData characterCardData;

    public CharacterCardData CharacterCardData => characterCardData;

    public int CurrentHealth => currentHealth;
    public int BaseHealth => baseHealth;
    public int CurrentAttack => currentAttack;
    public int BaseAttack => baseAttack;
    public int BaseSpeed => baseSpeed;
    public int CurrentSpeed => currentSpeed;
    public bool IsAlive => isAlive;

    // Delegate to CharacterCardData (read-only)
    public string CharacterName => characterCardData?.characterName ?? "";
    public string CharacterDescription => characterCardData?.characterDescription ?? "";
    public Sprite CharacterSprite => characterCardData?.characterSprite;
    public CharacterTeam Team => characterCardData?.characterTeam ?? CharacterTeam.Hero;
    public BigInteger Price => characterCardData?.price ?? 0;
    public Rarity Rarity => characterCardData?.rarity ?? Rarity.Rare;
    public int Level => level;

    // Legacy property for backward compatibility
    public string id => characterCardData?.characterName ?? "";


    public event Action HealthCurrentChanged;
    public event Action AttackCurrentChanged;
    public event Action OnSpriteChanged;

    public void Initialize(CharacterCardData characterCardData)
    {
        if (characterCardData == null)
        {
            return;
        }
        this.characterCardData = characterCardData;

        // Initialize base stats
        baseHealth = characterCardData.baseHealth;
        baseAttack = characterCardData.baseAttack;
        baseSpeed = characterCardData.baseSpeed;

        // Initialize current stats
        currentHealth = characterCardData.currentHealth;
        currentAttack = characterCardData.currentAttack;
        currentSpeed = characterCardData.currentSpeed;

        // Initialize other properties
        isAlive = true;
        level = characterCardData.level;
    }




    #region Current Health
    public void ChangeCurrentHealth(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, baseHealth);
        UpdateCurrentHealth();
    }
    public void RestoreCurrentHealth()
    {
        currentHealth = baseHealth;
        UpdateCurrentHealth();
    }
    public void UpdateCurrentHealth()
    {
        HealthCurrentChanged?.Invoke();
    }
    #endregion

    #region Sprite Region
    public void UpdateSprite(Sprite newSprite)
    {
        if (characterCardData != null)
        {
            characterCardData.characterSprite = newSprite;
            OnSpriteChanged?.Invoke();
        }
    }
    #endregion

    #region Current Attack
    public void IncrementCurrentAttack(int amount)
    {
        currentAttack += amount;
        UpdateCurrentAttack();
    }
    public void DecrementCurrentAttack(int amount)
    {
        currentAttack -= amount;
        UpdateCurrentAttack();
    }
    public void RestoreCurrentAttack()
    {
        if (characterCardData != null)
        {
            currentAttack = characterCardData.baseAttack;
        }
        UpdateCurrentAttack();
    }
    public void UpdateCurrentAttack()
    {
        AttackCurrentChanged?.Invoke();
    }
    #endregion


    public void Attack(ICharacter target, int bonusAttack)
    {
        if (!isAlive || target == null || !target.IsAlive)
            return;

        // Apply damage to the target
        target.TakeDamage(currentAttack + bonusAttack, this);
    }

    public void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;


        ChangeCurrentHealth(-damageAmount);

        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (!isAlive)
            return;
        ChangeCurrentHealth(healAmount);

    }

    public void Die()
    {
        currentHealth = 0;
        isAlive = false;
    }


}