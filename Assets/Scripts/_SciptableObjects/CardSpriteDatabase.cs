using UnityEngine;

[CreateAssetMenu(fileName = "CardSpriteDatabase", menuName = "Cards/Sprite Database")]
public class CardSpriteDatabase : ScriptableObject
{
    [Header("Standard Playing Cards")]
    public Sprite[] cardSprites; // 52 card sprites (13 ranks x 4 suits)

    [Header("Index-Based Cards")]
    public Sprite[] indexCardSprites; // Any number of card sprites for index-based cards

    // Method to get a standard card sprite by suit and rank
    public Sprite GetCardSprite(CardSuit suit, CardRank rank)
    {
        int suitIndex = (int)suit;
        int rankIndex = (int)rank - 2; // Two starts at 2
        int spriteIndex = suitIndex * 13 + rankIndex;

        if (spriteIndex >= 0 && spriteIndex < cardSprites.Length)
        {
            return cardSprites[spriteIndex];
        }
        else
        {
            Debug.LogWarning("Invalid sprite index: " + spriteIndex);
            return null;
        }
    }

    // Method to get an index card sprite
    public Sprite GetIndexCardSprite(int index)
    {
        if (index >= 0 && index < indexCardSprites.Length)
        {
            return indexCardSprites[index];
        }
        else
        {
            Debug.LogWarning("Invalid index card sprite index: " + index);
            return null;
        }
    }
}