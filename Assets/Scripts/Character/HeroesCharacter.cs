using Obvious.Soap.Example;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeroesCharacter : BaseCharacter
{
    [SerializeField] private int maxEnergy = 100;
    private int currentEnergy;
    /// <summary>
    /// Gets the current energy of the hero.
    /// </summary>
    public int Energy => currentEnergy;

    protected override void Awake()
    {
        base.Awake(); // Call base.Awake() first to initialize UIAct
        currentEnergy = maxEnergy;

        // Load hero data from GameSession if available
        if (GameSession.heroes != null)
        {
            SetData(GameSession.heroes);
        }
    }

    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        // Track damage taken
        GameStateManager.Instance?.TrackDamageTaken(damageAmount);

        // Apply damage
        currentHealth -= damageAmount;
        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.id}! Remaining HP: {HP}");
        GetComponent<Card>().OnCharacterDataChange();
        base.TakeDamage(damageAmount, attacker);
        
        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Override the Attack method to add hero-specific behavior
    /// </summary>
    public override void Attack(ICharacter target)
    {
        base.Attack(target);
        GetComponent<Card>().OnCharacterDataChange();
        // If the target died from this attack, gain experience
        if (target != null && !target.IsAlive() && target is EnemyCharacter enemy)
        {
            //GainExperience(enemy.ExperienceReward);
        }
        currentEnergy += 10; // Gain energy after attacking
        if(currentEnergy >= maxEnergy)
        {
            currentEnergy = 0;
        }
    }

    protected override void Die()
    {
        base.Die();
        GetComponent<Card>().OnCharacterDeath();
        
        // Trigger lose condition
        GameStateManager.Instance?.OnBattleLose();
    }
    public void SetData(HeroCardData heroData)
    {
        if (heroData == null) return;

        this.maxHealth = heroData.maxHealth;
        this.currentHealth = heroData.currentHealth;
        this.attackPower = heroData.attack;
        this.characterName = heroData.Name;
        this.sprite = heroData.Sprite;
    }
}
