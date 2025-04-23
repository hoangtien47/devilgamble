using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CardData
{
    public CardSuit suit;
    public CardRank rank;
    public bool isEnhanced;
    public int multiplier = 1;
    public bool isSealed;
    public int edition; // 0 = normal, 1 = holographic, 2 = polychrome, etc.

    public CardData(CardSuit s, CardRank r)
    {
        suit = s;
        rank = r;
        isEnhanced = false;
        multiplier = 1;
        isSealed = false;
        edition = 0;
    }
}

[System.Serializable]
public class HeroData
{
    public int index;
}

[System.Serializable]
public enum SortBy
{
    Rank,
    Suit,
}

public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform deckTransform;
    [SerializeField] private Transform discardTransform;
    [SerializeField] private HorizontalCardHolder handHolder;

    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private HorizontalCardHolder heroHolder;


    [Header("Deck Settings")]
    [SerializeField] private int handSize = 8;
    [SerializeField] private float dealDelay = 0.1f;
    [SerializeField] private float dealDuration = 0.3f;
    [SerializeField] private Ease dealEase = Ease.OutBack;
    [SerializeField] private bool autoShuffle = true;

    [Header("Balatro Settings")]
    [SerializeField] private int baseMultiplier = 1;
    [SerializeField] private int jokerSlots = 3;
    [SerializeField] private int handMultiplier = 1;

    [Header("State")]
    public List<CardData> deckCards = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public List<CardData> handCards = new List<CardData>();
    public List<CardData> removedCards = new List<CardData>(); // Cards removed from play
    public List<CardData> selectedCards = new List<CardData>(); // Selected cards

    private bool isDealing = false;

    [Header("Events")]
    public UnityEvent<int> OnDeckCountChanged;
    public UnityEvent<int> OnDiscardCountChanged;
    public UnityEvent<List<CardData>> OnHandDealt;
    public UnityEvent<CardData> OnCardDrawn;
    public UnityEvent OnDeckShuffled;
    public UnityEvent<int, int> OnScoreCalculated; // score, multiplier

    [Header("SortBy")]
    [SerializeField] private SortBy sortBy = SortBy.Suit;

    private void Start()
    {
        OnHandDealt.AddListener(OnHandDealtHandler);


        InitializeDeck();
        ShuffleDeck();
        DealHand();

        StartCoroutine(DealHeroCoroutine(3));
    }

    private void OnHandDealtHandler(List<CardData> hand)
    {
        // Sort the hand based on the selected sort method
        if (sortBy == SortBy.Rank)
        {
            print("Sorting by rank");
            SortByRank();
        }
        else if (sortBy == SortBy.Suit)
        {
            print("Sorting by suit");
            SortBySuit();
        }

        // Optionally, unsubscribe if you only want this to happen once
        // OnHandDealt.RemoveListener(OnHandDealtHandler);
    }

    public void InitializeDeck()
    {
        deckCards.Clear();
        discardPile.Clear();
        handCards.Clear();
        removedCards.Clear();
        selectedCards.Clear();

        // Create a standard 52-card deck
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                deckCards.Add(new CardData(suit, rank));
            }
        }

        OnDeckCountChanged?.Invoke(deckCards.Count);
    }

    public void ShuffleDeck()
    {
        // Fisher-Yates shuffle algorithm
        System.Random rng = new System.Random();
        int n = deckCards.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardData temp = deckCards[k];
            deckCards[k] = deckCards[n];
            deckCards[n] = temp;
        }

        OnDeckShuffled?.Invoke();
    }

    public void HandleCardSelection(Card card, bool isSelected)
    {
        int index = handHolder.cards.IndexOf(card);
        if (index >= 0 && index < handCards.Count)
        {
            CardData cardData = handCards[index];

            if (isSelected && !selectedCards.Contains(cardData))
            {
                selectedCards.Add(cardData);
            }
            else if (!isSelected && selectedCards.Contains(cardData))
            {
                selectedCards.Remove(cardData);
            }
        }
    }

    public void DealHand()
    {
        if (isDealing)
            return;

        if (handCards.Count == 0)
        {
            StartCoroutine(DealHandCoroutine(handSize));
        }
        else if (handCards.Count < handSize)
        {
            StartCoroutine(DealHandCoroutine(handSize - handCards.Count));
        }
        else
        {
            return;
        }

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (int i = 0; i < handHolder.cards.Count; i++)
            {
                if (handHolder.cards[i].cardVisual != null)
                    handHolder.cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }


    }

    private IEnumerator DealHeroCoroutine(int numCardDeal)
    {

        // Create slots for the hand
        for (int i = 0; i < numCardDeal; i++)
        {

            // Create a slot and card
            GameObject slot = Instantiate(heroPrefab, heroHolder.transform);
            Card card = slot.GetComponentInChildren<Card>();
            card.isCharacterCard = true;
            card.charIndex = i;
            yield return new WaitForSeconds(dealDelay);
        }

        // Update the card holder
        heroHolder.cards = heroHolder.GetComponentsInChildren<Card>().ToList();

        // Set up event listeners for the new cards
        int cardCount = 0;
        foreach (Card card in heroHolder.cards)
        {
            // Draw a card from the deck

            card.PointerEnterEvent.AddListener(heroHolder.CardPointerEnter);
            card.PointerExitEvent.AddListener(heroHolder.CardPointerExit);
            card.BeginDragEvent.AddListener(heroHolder.BeginDrag);
            card.EndDragEvent.AddListener(heroHolder.EndDrag);
            card.SelectEvent.AddListener(HandleCardSelection); // Add selection listener
            card.name = cardCount.ToString();
            cardCount++;
        }

    }

    private IEnumerator DealHandCoroutine(int numCardDeal)
    {
        isDealing = true;


        // Check if we need to reshuffle
        if (deckCards.Count < handSize && autoShuffle)
        {
            ReshuffleDiscardIntoDeck();
        }

        // Create slots for the hand
        for (int i = 0; i < numCardDeal; i++)
        {
            if (deckCards.Count == 0)
                break;


            // Create a slot and card
            GameObject slot = Instantiate(slotPrefab, handHolder.transform);

            CardData cardData = DrawCardFromDeck();
            handCards.Add(cardData);
            Card card = slot.GetComponentInChildren<Card>();
            card.Suit = cardData.suit;
            card.Rank = cardData.rank;

            yield return new WaitForSeconds(dealDelay);
        }

        // Update the card holder
        handHolder.cards = handHolder.GetComponentsInChildren<Card>().ToList();

        // Set up event listeners for the new cards
        int cardCount = 0;
        foreach (Card card in handHolder.cards)
        {
            // Draw a card from the deck

            card.PointerEnterEvent.AddListener(handHolder.CardPointerEnter);
            card.PointerExitEvent.AddListener(handHolder.CardPointerExit);
            card.BeginDragEvent.AddListener(handHolder.BeginDrag);
            card.EndDragEvent.AddListener(handHolder.EndDrag);
            card.SelectEvent.AddListener(HandleCardSelection); // Add selection listener
            card.name = cardCount.ToString();
            cardCount++;
        }

        OnHandDealt?.Invoke(handCards);
        isDealing = false;
    }

    public CardData DrawCardFromDeck()
    {
        if (deckCards.Count == 0)
        {
            if (autoShuffle && discardPile.Count > 0)
            {
                ReshuffleDiscardIntoDeck();
            }
            else
            {
                Debug.LogWarning("Attempted to draw from an empty deck!");
                return null;
            }
        }

        CardData drawnCard = deckCards[0];
        deckCards.RemoveAt(0);

        OnDeckCountChanged?.Invoke(deckCards.Count);
        OnCardDrawn?.Invoke(drawnCard);

        return drawnCard;
    }

    public void DiscardHand()
    {
        if (selectedCards.Count > 0)
        {
            StartCoroutine(DiscardSelectedCardsCoroutine());
        }
        else
        {
            StartCoroutine(DiscardHandCoroutine());
        }
    }

    private IEnumerator DiscardSelectedCardsCoroutine()
    {
        if (handHolder.cards == null || handHolder.cards.Count == 0)
            yield break;

        List<Card> cardsToDiscard = new List<Card>();
        List<int> indicesToRemove = new List<int>();

        // Find all selected cards
        for (int i = 0; i < handHolder.cards.Count; i++)
        {
            Card card = handHolder.cards[i];
            if (card != null && card.selected)
            {
                cardsToDiscard.Add(card);
                indicesToRemove.Add(i);
            }
        }

        // If no cards are selected, exit
        if (cardsToDiscard.Count == 0)
            yield break;

        // Animate cards to discard pile
        foreach (Card card in cardsToDiscard)
        {
            // Animate card to discard pile
            card.transform.DOMove(discardTransform.position, dealDuration)
                .SetEase(Ease.InBack);

            // Add card data to discard pile
            int index = handHolder.cards.IndexOf(card);
            if (index >= 0 && index < handCards.Count)
            {
                discardPile.Add(handCards[index]);
            }

            yield return new WaitForSeconds(dealDelay / 2);
        }

        yield return new WaitForSeconds(dealDuration);

        // Remove the cards from the hand (in reverse order to avoid index issues)
        indicesToRemove.Sort();
        indicesToRemove.Reverse();
        foreach (int index in indicesToRemove)
        {
            if (index < handCards.Count)
            {
                handCards.RemoveAt(index);
            }
        }

        // Destroy the selected card objects
        foreach (Card card in cardsToDiscard)
        {
            if (card != null && card.transform.parent != null)
            {
                Destroy(card.transform.parent.gameObject);
            }
        }

        selectedCards.Clear();

        // Update the card holder
        handHolder.cards = handHolder.GetComponentsInChildren<Card>().ToList();

        DealHand();

        OnDiscardCountChanged?.Invoke(discardPile.Count);
    }

    private IEnumerator DiscardHandCoroutine()
    {
        if (handHolder.cards == null || handHolder.cards.Count == 0)
            yield break;


        // Animate cards to discard pile
        foreach (Card card in handHolder.cards)
        {
            if (card == null)
                continue;

            // Animate card to discard pile
            card.transform.DOMove(discardTransform.position, dealDuration)
                .SetEase(Ease.InBack);

            // Add card data to discard pile
            int index = handHolder.cards.IndexOf(card);
            if (index >= 0 && index < handCards.Count)
            {
                discardPile.Add(handCards[index]);
            }

            yield return new WaitForSeconds(dealDelay / 2);
        }

        yield return new WaitForSeconds(dealDuration);

        // Clear the hand
        ClearHand();
        handCards.Clear();

        OnDiscardCountChanged?.Invoke(discardPile.Count);
        DealHand();
    }

    public void ClearHand()
    {
        // Destroy all card objects in the hand
        foreach (Transform child in handHolder.transform)
        {
            Destroy(child.gameObject);
        }

        handHolder.cards.Clear();
    }

    public void ReshuffleDiscardIntoDeck()
    {
        deckCards.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();

        OnDeckCountChanged?.Invoke(deckCards.Count);
        OnDiscardCountChanged?.Invoke(0);
    }

    public void RemoveCardFromDeck(CardData cardData)
    {
        // Find and remove the card from the deck
        int index = deckCards.FindIndex(c => c.suit == cardData.suit && c.rank == cardData.rank);
        if (index >= 0)
        {
            removedCards.Add(deckCards[index]);
            deckCards.RemoveAt(index);
            OnDeckCountChanged?.Invoke(deckCards.Count);
        }
    }

    public void EnhanceCard(Card card, int multiplierBonus = 1)
    {
        int index = handHolder.cards.IndexOf(card);
        if (index >= 0 && index < handCards.Count)
        {
            handCards[index].isEnhanced = true;
            handCards[index].multiplier += multiplierBonus;

            // Visual feedback
            card.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1);

            // You might want to update the card visual to show enhancement
            if (card.cardVisual != null)
            {
                // Update visual to show enhancement (this would need to be implemented in CardVisual)
            }
        }
    }

    public void CalculateScoreWithCombos()
    {
        if (selectedCards.Count < 5)
        {
            Debug.LogWarning("No cards selected for scoring.");
            return;
        }

        var hand = selectedCards;

        int baseScore = 0;
        float comboMultiplier = 1f;

        bool isFlush = hand.All(c => c.suit == hand[0].suit);
        bool isStraight = IsStraight(hand);
        var rankGroups = hand.GroupBy(c => c.rank).ToList();
        var groupSizes = rankGroups.Select(g => g.Count()).OrderByDescending(x => x).ToList();

        // Determine combo
        if (isStraight && isFlush)
        {
            baseScore = 500;
            comboMultiplier = 5f;
            Debug.Log("Straight Flush");
        }
        else if (groupSizes[0] == 4)
        {
            baseScore = 400;
            comboMultiplier = 4f;
            Debug.Log("Four of a Kind");
        }
        else if (groupSizes[0] == 3 && groupSizes.Count >= 2 && groupSizes[1] >= 2)
        {
            baseScore = 300;
            comboMultiplier = 3.5f;
            Debug.Log("Full House");
        }
        else if (isFlush)
        {
            baseScore = 250;
            comboMultiplier = 3f;
            Debug.Log("Flush");
        }
        else if (isStraight)
        {
            baseScore = 200;
            comboMultiplier = 2.5f;
            Debug.Log("Straight");
        }
        else if (groupSizes[0] == 3)
        {
            baseScore = 150;
            comboMultiplier = 2f;
            Debug.Log("Three of a Kind");
        }
        else if (groupSizes.Count(g => g == 2) == 2)
        {
            baseScore = 100;
            comboMultiplier = 2f;
            Debug.Log("Two Pair");
        }
        else if (groupSizes[0] == 2)
        {
            baseScore = 50;
            comboMultiplier = 1.5f;
            Debug.Log("Pair");
        }
        else
        {
            baseScore = 5 * hand.Count;
            comboMultiplier = 1f;
            Debug.Log("High Card");
        }

        // Enhance bonuses
        foreach (var card in hand)
        {
            if (card.isEnhanced)
            {
                comboMultiplier += 0.1f * card.multiplier;
            }
        }

        int finalScore = Mathf.RoundToInt(baseScore * comboMultiplier * handMultiplier);
        OnScoreCalculated?.Invoke(finalScore, Mathf.RoundToInt(comboMultiplier * handMultiplier));
        Debug.Log($"Final Score: {finalScore} (Base: {baseScore} x Multiplier: {comboMultiplier} x HandMultiplier: {handMultiplier})");
        StartCoroutine(DiscardSelectedCardsCoroutine());

    }

    private bool IsStraight(List<CardData> cards)
    {
        var sorted = cards.Select(c => (int)c.rank).Distinct().OrderBy(n => n).ToList();

        if (sorted.Count < 5)
            return false;

        for (int i = 0; i <= sorted.Count - 5; i++)
        {
            bool isSeq = true;
            for (int j = 0; j < 4; j++)
            {
                if (sorted[i + j + 1] != sorted[i + j] + 1)
                {
                    isSeq = false;
                    break;
                }
            }

            if (isSeq)
                return true;
        }

        return false;
    }

    public void SortByRank()
    {
        if (handHolder.cards == null || handHolder.cards.Count == 0)
            return;

        sortBy = SortBy.Rank;

        // Create a list of card-index pairs
        List<(Card card, int index, CardData data)> cardPairs = new List<(Card, int, CardData)>();
        for (int i = 0; i < handHolder.cards.Count; i++)
        {
            if (i < handCards.Count)
            {
                cardPairs.Add((handHolder.cards[i], i, handCards[i]));
            }
        }

        // Sort by rank
        cardPairs = cardPairs.OrderBy(pair => (int)pair.data.rank).ToList();

        // Rearrange cards in the UI
        RearrangeCards(cardPairs);
    }

    public void SortBySuit()
    {
        if (handHolder.cards == null || handHolder.cards.Count == 0)
            return;

        sortBy = SortBy.Suit;

        // Create a list of card-index pairs
        List<(Card card, int index, CardData data)> cardPairs = new List<(Card, int, CardData)>();
        for (int i = 0; i < handHolder.cards.Count; i++)
        {
            if (i < handCards.Count)
            {
                cardPairs.Add((handHolder.cards[i], i, handCards[i]));
            }
        }

        // Sort by suit, then by rank
        cardPairs = cardPairs.OrderBy(pair => (int)pair.data.suit)
                             .ThenBy(pair => (int)pair.data.rank)
                             .ToList();

        // Rearrange cards in the UI
        RearrangeCards(cardPairs);
    }

    private void RearrangeCards(List<(Card card, int index, CardData data)> sortedPairs)
    {
        // Temporarily store the cards and their data
        List<CardData> newHandCards = new List<CardData>();
        List<Transform> cardSlots = new List<Transform>();

        // Get all the card slots (parents of cards)
        foreach (var pair in sortedPairs)
        {
            cardSlots.Add(pair.card.transform.parent);
            newHandCards.Add(pair.data);
        }

        // Rearrange the cards in the UI
        for (int i = 0; i < sortedPairs.Count; i++)
        {
            // Set the sibling index to reorder in hierarchy
            cardSlots[i].SetSiblingIndex(i);
        }

        // Update the handCards list to match the new order
        handCards = newHandCards;

        // Update the cards list in the handHolder
        handHolder.cards = handHolder.GetComponentsInChildren<Card>().ToList();

        // Update card visuals
        StartCoroutine(UpdateCardVisuals());
    }

    private IEnumerator UpdateCardVisuals()
    {
        yield return new WaitForEndOfFrame();

        // Update visual indices
        for (int i = 0; i < handHolder.cards.Count; i++)
        {
            if (handHolder.cards[i].cardVisual != null)
            {
                handHolder.cards[i].cardVisual.UpdateIndex(handHolder.transform.childCount);
            }
        }
    }

}