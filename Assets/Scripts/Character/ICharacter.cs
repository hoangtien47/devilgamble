using UnityEngine;

/// <summary>
/// Interface that defines what a character in the game should have
/// </summary>
public interface ICharacter
{
    string id{ get; }
    int HP { get; }
    int ATK { get; }

    void Attack(ICharacter target);
    void TakeDamage(int damageAmount, ICharacter attacker);
    bool IsAlive();
}

