/// <summary>
/// Interface that defines what a character in the game should have
/// </summary>
public interface ICharacter
{
    // Static data (read-only) - delegate to CharacterCardData
    string CharacterName { get; }
    string CharacterDescription { get; }
    CharacterTeam Team { get; }


    // Runtime data (read-write) - keep these
    int CurrentHealth { get; }
    int BaseHealth { get; }
    int CurrentAttack { get; }
    int BaseAttack { get; }
    int BaseSpeed { get; }
    int CurrentSpeed { get; }
    bool IsAlive { get; }

    void Attack(ICharacter target, int bonusAttack);
    void TakeDamage(int damageAmount, ICharacter attacker);
    void Heal(int healAmount);
    void Die();
}

