using UnityEngine;

/// <summary>
/// Stub class for CharacterCard to fix missing references
/// This should be replaced with the actual CharacterCard implementation
/// </summary>
public class CharacterCard : MonoBehaviour
{
    public virtual void OnCharacterDataChange()
    {
        Debug.Log($"Character data changed for {gameObject.name}");
    }

    public virtual void OnCharacterDeath()
    {
        Debug.Log($"Character {gameObject.name} has died");
    }
}
