using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroCard", menuName = "CharacterCard/Hero Card")]

public class HeroCardScriptable : CharacterCardScriptable
{
    [SerializeField]private bool IsUnlocked = false; // Default to locked
    public bool isUnlocked
    {
        get { return IsUnlocked; }
        set { IsUnlocked = value; }
    }
}
