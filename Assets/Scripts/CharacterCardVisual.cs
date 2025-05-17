using TMPro;
using UnityEngine;
public class CharacterCardVisual : CardVisual
{
    [Header("==========UI Character==========")]
    [SerializeField] private TextMeshProUGUI _HPText;
    [SerializeField] private TextMeshProUGUI _ATKText;
    [SerializeField] private TextMeshProUGUI _NameText;
    public override void Initialize(Card target)
    {
        // Call base implementation first to set up common functionality and event listeners
        base.Initialize(target);

        //cardImage.sprite = characterCardScriptable.Sprite;
    }
    public override void OnChangeData(int HP, int ATK)
    {
        base.OnChangeData(HP, ATK);
        _HPText.SetText(HP.ToString());
        _ATKText.SetText(ATK.ToString());
    }
    public override void OnLoadCharacter(BaseCharacter character)
    {
        base.OnLoadCharacter(character);
        if (character == null) return;
        _HPText.SetText(character.HP.ToString());
        _ATKText.SetText(character.ATK.ToString());
        _NameText.SetText(character.Name);
        cardImage.sprite = character.Sprite;
    }
}
