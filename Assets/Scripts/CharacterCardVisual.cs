using UnityEngine;
public class CharacterCardVisual : CardVisual
{
    [Header("Specific Sprite")]
    [SerializeField] private CharacterCardScriptable characterCardScriptable;

    public override void Initialize(Card target)
    {
        // Call base implementation first to set up common functionality and event listeners
        base.Initialize(target);

        cardImage.sprite = characterCardScriptable.Sprite;
    }
}
