using Obvious.Soap.Example;
using TMPro;
using UnityEngine;

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
    }

    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        base.TakeDamage(damageAmount, attacker); // Call base.TakeDamage first to show popup

        // Apply damage
        currentHealth -= damageAmount;
        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.id}! Remaining HP: {HP}");
        GetComponent<Card>().OnCharacterDataChange();
        
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
    }
    protected override void Die()
    {
        base.Die();
    }
    public void SetData(HeroCardScriptable hero)
    {
        this.maxHealth = hero.health;
        this.currentHealth = hero.health;
        this.attackPower = hero.attack;
        this.characterName = hero.Name;
        this.sprite = hero.Sprite;
        sprite = hero.Sprite;
    }
}
