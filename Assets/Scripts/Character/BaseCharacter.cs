using TMPro;
using UnityEngine;

/// <summary>
/// Base MonoBehaviour class that implements the ICharacter interface
/// This provides common functionality for all character types
/// </summary>
public abstract class BaseCharacter : MonoBehaviour, ICharacter
{
    [SerializeField] protected string idCharacter;
    [SerializeField] protected string characterName;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int attackPower = 10;
    [SerializeField] private int attackAmount = 0;
    [SerializeField] protected Sprite sprite;

    protected int currentHealth;
    protected bool isAlive = true;

    private UIAct uiAct;

    // ICharacter interface implementation
    public string id => idCharacter;
    public string Name => characterName;
    public int HP => currentHealth;
    public int ATK => attackPower;
    public UIAct ui => uiAct;
    public Sprite Sprite => sprite;

    protected virtual void Awake()
    {
        // Initialize health to max at start
        currentHealth = maxHealth;
        uiAct = GetComponent<UIAct>();
    }

    /// <summary>
    /// Performs an attack on the target character
    /// </summary>
    public virtual void Attack(ICharacter target)
    {
        if (!isAlive || target == null || !target.IsAlive())
            return;

        // Apply damage to the target
        target.TakeDamage(attackAmount, this);

        Debug.Log($"{idCharacter} attacks {target.id} for {ATK} damage!");
    }

    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public virtual void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;
        
        if (uiAct != null)  // Add null check for UIAct
        {
            uiAct.ShowPopup(damageAmount, false); // Show damage popup
        }
        
        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles character death
    /// </summary>
    protected virtual void Die()
    {
        currentHealth = 0;
        isAlive = false;
        Debug.Log($"{idCharacter} has died!");
    }

    /// <summary>
    /// Checks if the character is alive
    /// </summary>
    public bool IsAlive()
    {
        return isAlive;
    }
    public void SetAttack(int attack)
    {
        attackAmount =  attack;
    }
}
