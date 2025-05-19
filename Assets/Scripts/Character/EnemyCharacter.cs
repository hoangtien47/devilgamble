using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyCharacter : BaseCharacter
{

    [Header("====Enemy Specific====")]
    [SerializeField] private int goldReward = 10;

    // Stamina attribute
    [SerializeField] private int maxStamina = 100;
    private int currentStamina;
    // Turn attribute
    [SerializeField] private int turn = 3;
    /// <summary>
    /// Override the Attack method to add enemy-specific behavior
    /// </summary>
    public override void Attack(ICharacter target)
    {
        if (!isAlive || target == null || !target.IsAlive())
            return;

        // Apply damage to the target
        target.TakeDamage(attackPower, this);
        Debug.Log($"{idCharacter} attacks {target.id} for {ATK} damage!");
    }
    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        // Calculate damage reduction based on stamina
        if (currentStamina > 0)
        {
            // Reduce damage by 30% if stamina is above 0
            damageAmount = Mathf.RoundToInt(damageAmount * 0.7f);
            // Reduce stamina by a percentage of the damage taken
            currentStamina -= Mathf.RoundToInt(damageAmount * 0.1f); // 10% of damage taken
            currentStamina = Mathf.Max(currentStamina, 0); // Ensure stamina doesn't go below 0
        }

        // Track damage dealt by hero
        GameStateManager.Instance?.TrackDamageDealt(damageAmount);

        // Apply damage
        currentHealth -= damageAmount;

        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.id}! Remaining HP: {HP}, Stamina: {currentStamina}");
        GetComponent<Card>().OnCharacterDataChange();
        base.TakeDamage(damageAmount, attacker);
        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    /// <summary>
    /// Override the Die method to add enemy-specific behavior
    /// </summary>
    protected override void Die()
    {
        base.Die();
        GetComponent<Card>().OnCharacterDeath();
        
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
    public void SetData(EnemyCardScriptable enemy)
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
