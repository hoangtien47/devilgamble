using TMPro;
using UnityEngine;

public class HeroesCharacter : BaseCharacter
{
    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public override void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

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
}
