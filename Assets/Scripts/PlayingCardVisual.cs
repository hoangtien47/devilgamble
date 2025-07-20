using UnityEngine;

public class PlayingCardVisual : CardVisual
{
    private CardSuit Suit;
    private CardRank Rank;

    [Header("Card Sprites")]
    public CardSpriteDatabase spriteDatabase;

    public override void Initialize(Card target)
    {
        base.Initialize(target);

        Suit = target.Suit;
        Rank = target.Rank;

        int suitIndex = (int)Suit;
        int rankIndex = (int)Rank - 2;
        int spriteIndex = suitIndex * 13 + rankIndex;


        if (spriteIndex >= 0 && spriteIndex < spriteDatabase.cardSprites.Length)
        {
            cardImage.sprite = spriteDatabase.GetCardSprite(Suit, Rank);
        }
        else
        {
            Debug.LogWarning("Invalid sprite index: " + spriteIndex);
        }

    }
}
