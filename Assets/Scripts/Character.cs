using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public string characterName;
    public string elementType;
    public string role;  // Class/Role
    public int rarity;   // 3-5 stars

    // Base Stats
    public int baseHP;
    public int baseAttack;
    public int baseDefense;
    //public float critRate;
    //public float critDamage;

    // Current Stats (modified during battle)
    [HideInInspector] public int currentHP;
    [HideInInspector] public int currentAttack;
    [HideInInspector] public int currentDefense;
    //[HideInInspector] public float currentCritRate;
    //[HideInInspector] public float currentCritDamage;

    // Resources
    //[HideInInspector] public int energy;
    //[HideInInspector] public int maxEnergy = 100;

    // Status
    [HideInInspector] public List<StatusEffect> statusEffects = new List<StatusEffect>();

    public void ResetStats()
    {
        currentHP = baseHP;
        currentAttack = baseAttack;
        currentDefense = baseDefense;
        //currentSpeed = baseSpeed;
        //currentCritRate = critRate;
        //currentCritDamage = critDamage;
        //energy = 0;
    }
}

[System.Serializable]
public class Ability
{
    public string abilityName;
    [TextArea(3, 5)]
    public string description;
    public int skillPointCost;
    public int cooldown;
    public int currentCooldown;
    public Sprite icon;

    // For card representation
    public int cardCost;
    public string cardType; // "Attack", "Skill", "Support", "Ultimate"
    public int cardRarity; // 1-5
}

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public string effectType; // "Buff", "Debuff"
    public int duration;
    public StatModifier[] modifiers;

    [System.Serializable]
    public class StatModifier
    {
        public string statToModify; // "HP", "Attack", "Defense", etc.
        public float value;
        public bool isPercentage;
    }
}

public class Character : MonoBehaviour
{
    public CharacterStats stats;

    // Abilities
    public Ability basicAttack;
    public Ability skill;
    public Ability ultimate;
    public Ability talent; // Passive
    public Ability technique; // Overworld ability

    // Visual elements
    public Sprite characterPortrait;
    public GameObject characterModel;

    // Animation references
    public Animator animator;

    //protected BattleManager battleManager;

    private void Awake()
    {
        //battleManager = FindObjectOfType<BattleManager>();
        stats.ResetStats();
    }

    // Core battle methods
    public virtual int PerformBasicAttack(Character target)
    {
        // Generate 1 skill point
        //battleManager.AddSkillPoints(1);

        // Calculate damage
        int damage = CalculateDamage(stats.currentAttack, target.stats.currentDefense);

        // Apply damage
        target.TakeDamage(damage);

        // Play animation
        animator.SetTrigger("BasicAttack");

        return damage;
    }

    public virtual int PerformSkill(Character target)
    {
        // Check and consume skill points
        //if (battleManager.CurrentSkillPoints < skill.skillPointCost)
        //    return 0;

        //battleManager.UseSkillPoints(skill.skillPointCost);

        // Calculate damage (example: skill does 150% of attack)
        int damage = CalculateDamage((int)(stats.currentAttack * 1.5f), target.stats.currentDefense);

        // Apply damage
        target.TakeDamage(damage);

        // Set cooldown
        skill.currentCooldown = skill.cooldown;

        // Play animation
        animator.SetTrigger("Skill");

        return damage;
    }

    //public virtual int PerformUltimate(List<Character> targets)
    //{
    //    // Check energy
    //    if (stats.energy < stats.maxEnergy)
    //        return 0;

    //    // Reset energy
    //    stats.energy = 0;

    //    int totalDamage = 0;

    //    // Apply effect to all targets (example: AOE damage)
    //    foreach (Character target in targets)
    //    {
    //        int damage = CalculateDamage((int)(stats.currentAttack * 3f), target.stats.currentDefense);
    //        target.TakeDamage(damage);
    //        totalDamage += damage;
    //    }

    //    // Play animation
    //    animator.SetTrigger("Ultimate");

    //    return totalDamage;
    //}

    public virtual void TakeDamage(int damage)
    {
        stats.currentHP -= damage;
        if (stats.currentHP < 0)
            stats.currentHP = 0;

        // Check if defeated
        if (stats.currentHP <= 0)
        {
            Die();
        }

        // Play hit animation
        animator.SetTrigger("Hit");
    }

    public virtual void Heal(int amount)
    {
        stats.currentHP += amount;
        if (stats.currentHP > stats.baseHP)
            stats.currentHP = stats.baseHP;
    }

    //public virtual void AddEnergy(int amount)
    //{
    //    stats.energy += amount;
    //    if (stats.energy > stats.maxEnergy)
    //        stats.energy = stats.maxEnergy;
    //}

    public virtual void Die()
    {
        // Play death animation
        animator.SetTrigger("Die");

        // Notify battle manager
        //battleManager.CharacterDefeated(this);
    }

    // Helper methods
    protected virtual int CalculateDamage(int attackValue, int defenseValue)
    {
        // Basic damage formula
        float damageReduction = defenseValue / (defenseValue + 500f); // Example formula
        int damage = Mathf.RoundToInt(attackValue * (1 - damageReduction));

        // Critical hit check
        //if (Random.Range(0f, 1f) < stats.currentCritRate)
        //{
        //    damage = Mathf.RoundToInt(damage * (1 + stats.currentCritDamage));
        //}

        return damage;
    }

    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        stats.statusEffects.Add(effect);

        // Apply immediate stat changes
        foreach (var modifier in effect.modifiers)
        {
            ApplyStatModifier(modifier);
        }
    }

    protected virtual void ApplyStatModifier(StatusEffect.StatModifier modifier)
    {
        switch (modifier.statToModify)
        {
            case "HP":
                if (modifier.isPercentage)
                    stats.currentHP += Mathf.RoundToInt(stats.baseHP * modifier.value);
                else
                    stats.currentHP += Mathf.RoundToInt(modifier.value);
                break;
            case "Attack":
                if (modifier.isPercentage)
                    stats.currentAttack += Mathf.RoundToInt(stats.baseAttack * modifier.value);
                else
                    stats.currentAttack += Mathf.RoundToInt(modifier.value);
                break;
                // Add other stats as needed
        }
    }

    public virtual void ProcessTurnStart()
    {
        // Process status effects
        for (int i = stats.statusEffects.Count - 1; i >= 0; i--)
        {
            stats.statusEffects[i].duration--;
            if (stats.statusEffects[i].duration <= 0)
            {
                // Remove expired effect
                stats.statusEffects.RemoveAt(i);
            }
        }

        // Reduce cooldowns
        if (skill.currentCooldown > 0)
            skill.currentCooldown--;
    }
}