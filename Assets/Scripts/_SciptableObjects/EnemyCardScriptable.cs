using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyCard", menuName = "CharacterCard/Enemy Card")]

public class EnemyCardScriptable : CharacterCardScriptable
{
    public int actionTurns; // Number of turns before the enemy attacks
}
