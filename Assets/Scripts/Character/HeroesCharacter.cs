using UnityEngine;

public class HeroesCharacter : BaseCharacter
{

    protected override void Awake()
    {
        base.Awake(); // Call base.Awake() first to initialize UIAct

        // Load hero data from GameSession if available
        if (GameSession.heroes != null)
        {
            SetData(GameSession.heroes);
        }
    }

    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        GameStateManager.Instance?.TrackDamageTaken(damageAmount);

        // Apply damage
        currentHealth -= damageAmount;
        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.id}! Remaining HP: {HP}");
        GetComponent<CharacterCard>().OnCharacterDataChange();
        base.TakeDamage(damageAmount, attacker);

        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public override void Attack(ICharacter target)
    {
        base.Attack(target);
        GetComponent<CharacterCard>().OnCharacterDataChange();
        // If the target died from this attack, gain experience
        if (target != null && !target.IsAlive() && target is EnemyCharacter enemy)
        {
        }
    }

    protected override void Die()
    {
        base.Die();
        GetComponent<CharacterCard>().OnCharacterDeath();

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
