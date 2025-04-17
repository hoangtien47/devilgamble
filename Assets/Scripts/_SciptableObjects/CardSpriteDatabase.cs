using UnityEngine;

[CreateAssetMenu(fileName = "CardSpriteDatabase", menuName = "Cards/Card Sprite Database")]
public class CardSpriteDatabase : ScriptableObject
{
    public Sprite[] cardSprites; // Order: Hearts 2-A, Diamonds 2-A, Clubs 2-A, Spades 2-A
}
