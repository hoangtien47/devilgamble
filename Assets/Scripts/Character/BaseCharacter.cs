using UnityEngine;

/// <summary>
/// Base MonoBehaviour class that implements the ICharacter interface
/// This provides common functionality for all character types
/// </summary>
public abstract class BaseCharacter : MonoBehaviour, ICharacter
{
    [SerializeField] protected string idCharacter;
    [SerializeField] protected string characterName;
    [SerializeField] protected string characterDescription;
    [SerializeField] protected CharacterTeam characterTeam;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int attackPower = 10;
    [SerializeField] protected int baseSpeed = 5;
    [SerializeField] protected Sprite sprite;

    protected int currentHealth;
    protected int currentAttack;
    protected int currentSpeed;
    protected bool isAlive = true;

    private UIAct uiAct;

    // ICharacter interface implementation
    public string CharacterName => characterName;
    public string CharacterDescription => characterDescription;
    public CharacterTeam Team => characterTeam;
    public int CurrentHealth => currentHealth;
    public int BaseHealth => maxHealth;
    public int CurrentAttack => currentAttack;
    public int BaseAttack => attackPower;
    public int BaseSpeed => baseSpeed;
    public int CurrentSpeed => currentSpeed;
    public bool IsAlive => isAlive;

    // Legacy properties for backward compatibility
    public string id => idCharacter;
    public string Name => characterName;
    public int HP => currentHealth;
    public int ATK => attackPower;
    public UIAct ui => uiAct;
    public Sprite Sprite => sprite;

    protected virtual void Awake()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentAttack = attackPower;
        currentSpeed = baseSpeed;
        uiAct = GetComponent<UIAct>();
    }

    /// <summary>
    /// Performs an attack on the target character
    /// </summary>
    public virtual void Attack(ICharacter target, int bonusAttack = 0)
    {
        if (!isAlive || target == null || !target.IsAlive)
            return;

        // Apply damage to the target
        target.TakeDamage(currentAttack + bonusAttack, this);

        Debug.Log($"{idCharacter} attacks {target.CharacterName} for {currentAttack + bonusAttack} damage!");
    }

    /// <summary>
    /// Legacy attack method for backward compatibility
    /// </summary>
    public virtual void Attack(ICharacter target)
    {
        Attack(target, 0);
    }

    /// <summary>
    /// Takes damage from an attacker
    /// </summary>
    public virtual void TakeDamage(int damageAmount, ICharacter attacker)
    {
        if (!isAlive)
            return;

        // Apply damage
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (uiAct != null)  // Add null check for UIAct
        {
            uiAct.ShowPopup(damageAmount, false); // Show damage popup
        }

        Debug.Log($"{idCharacter} takes {damageAmount} damage from {attacker.CharacterName}! Remaining HP: {currentHealth}");

        // Check if character died
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the character
    /// </summary>
    public virtual void Heal(int healAmount)
    {
        if (!isAlive)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"{idCharacter} heals for {healAmount}! Current HP: {currentHealth}");
    }

    /// <summary>
    /// Handles character death
    /// </summary>
    public virtual void Die()
    {
        currentHealth = 0;
        isAlive = false;
        Debug.Log($"{idCharacter} has died!");
    }
}
