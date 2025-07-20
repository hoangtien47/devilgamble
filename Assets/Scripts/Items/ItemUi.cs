using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUi : MonoBehaviour
{
    public Image iconItem;
    public TextMeshProUGUI amountText;
    
    public void SetUpItem(LevelReward reward)
    {
        iconItem.sprite = reward.RewardSprite;
        amountText.text = "x"+ reward.Amount.ToString();
    }
}
