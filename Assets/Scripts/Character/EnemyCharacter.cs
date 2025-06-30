using UnityEngine;

public class EnemyCharacter : BaseCharacter
{

    [Header("====Enemy Specific====")]
    [SerializeField] private int goldReward = 10;

    // Stamina attribute
    [SerializeField] private int maxStamina = 100;
    private int currentStamina;
    // Turn attribute
    [SerializeField] private int turn = 3;


    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        if (currentStamina > 0)
        {
            damageAmount = Mathf.RoundToInt(damageAmount * 0.7f);
            currentStamina -= Mathf.RoundToInt(damageAmount * 0.1f);
            currentStamina = Mathf.Max(currentStamina, 0);
        }

        // Track damage dealt by hero
        GameStateManager.Instance?.TrackDamageDealt(damageAmount);

        // Apply damage
        currentHealth -= damageAmount;

        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.id}! Remaining HP: {HP}, Stamina: {currentStamina}");
        GetComponent<CharacterCard>().OnCharacterDataChange();
        base.TakeDamage(damageAmount, attacker);
        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected override void Die()
    {
        base.Die();
        GetComponent<CharacterCard>().OnCharacterDeath();

        // Trigger win condition
        GameStateManager.Instance?.OnBattleWin();

        // Save current hero data before dropping rewards
        SaveHeroData();

        // Drop rewards and handle other death logic
        DropRewards();
    }

    /// <summary>
    /// Handles dropping rewards when enemy dies
    /// </summary>
    private void DropRewards()
    {
        Debug.Log($"{idCharacter} was defeated! Drops: {goldReward} gold.");
        // Logic for spawning items or giving rewards to the player would go here
    }

    public void SetData(EnemyCardData enemy)
    {
        this.maxHealth = enemy.maxHealth;
        this.currentHealth = enemy.currentHealth;
        this.attackPower = enemy.attack;
        this.characterName = enemy.Name;
        this.sprite = enemy.Sprite;
        this.turn = turn;
    }
    private void SaveHeroData()
    {
        // Find the hero character
        var hero = FindObjectOfType<HeroesCharacter>();
        if (hero != null && GameSession.heroes != null)
        {
            // Update hero's current health in the GameSession
            GameSession.heroes.SetData(hero);
            // You can add more stats to save here if needed
        }
    }
}
