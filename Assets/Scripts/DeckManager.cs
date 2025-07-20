using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    [SerializeField] private Transform discardTransform;
    [SerializeField] private Transform playcardTransform;
    [SerializeField] private HorizontalCardHolder handHolder;

    [Header("Hero Card")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private HorizontalCardHolder heroHolder;
    private Card selectedHeroCard = null;

    [Header("Enemy Card")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private HorizontalCardHolder enemyHolder;

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
    [SerializeField] private int discardChangeCount = 2;

    [Header("State")]
    public List<CardData> deckCards = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public List<CardData> handCards = new List<CardData>();
    public List<CardData> selectedCards = new List<CardData>(); // Selected cards
    public List<CardData> cardCombo = new List<CardData>();

    private bool isDealing = false;
    public bool canPlayCards = false;

    [Header("Events")]
    public UnityEvent<int> OnDeckCountChanged;
    public UnityEvent<int> OnDiscardCountChanged;
    public UnityEvent<List<CardData>> OnHandDealt;
    public UnityEvent<CardData> OnCardDrawn;
    public UnityEvent OnDeckShuffled;
    public UnityEvent<int, int> OnScoreCalculated; // score, multiplier

    [Header("SortBy")]
    [SerializeField] private SortBy sortBy = SortBy.Suit;

    [Header("Attack Settings")]
    [SerializeField] private Transform bossTransform;
    [SerializeField] private Transform heroTransform;
    [SerializeField] private float delayBetweenAttacks = 0.2f;
    [SerializeField] private bool useSpecialAttacks = false;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI discardChangeText;

    [Header("Public Getter for Character Card Holder")]
    public HorizontalCardHolder HeroHolder => heroHolder;
    public HorizontalCardHolder EnemyHolder => enemyHolder;

    public int finalScore = 0;

    private void Start()
    {
        // Reload current scene
        OnHandDealt.AddListener(OnHandDealtHandler);
        FadeOutScoreText();

        InitializeDeck();
        ShuffleDeck();
        DealHand();
        discardChangeText.text = discardChangeCount.ToString();
        StartCoroutine(DealEnemyCoroutine(1));
        StartCoroutine(DealHeroCoroutine(1));
    }

    private void OnHandDealtHandler(List<CardData> hand)
    {
        // Sort the hand based on the selected sort method
        if (sortBy == SortBy.Rank)
        {
            SortByRank();
        }
        else if (sortBy == SortBy.Suit)
        {
            SortBySuit();
        }

    }

    public void InitializeDeck()
    {
        deckCards.Clear();
        discardPile.Clear();
        handCards.Clear();
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
    public void HandleHeroCardSelection(Card card, bool isSelected)
    {
        if (isSelected)
        {
            // Deselect previous hero card if any
            if (selectedHeroCard != null && selectedHeroCard != card)
            {
                selectedHeroCard.Deselect();
            }
            selectedHeroCard = card;
        }
        else
        {
            if (selectedHeroCard == card)
                selectedHeroCard = null;
        }
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
                if (selectedCards.Count > 5)
                {
                    selectedCards.Remove(cardData);
                    card.Deselect();
                }
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
    private IEnumerator DealEnemyCoroutine(int numCardDeal)
    {
        // Create slots for the hand
        for (int i = 0; i < numCardDeal; i++)
        {
            // Create a slot and card
            GameObject slot = Instantiate(enemyPrefab, enemyHolder.transform);
            Card card = slot.GetComponentInChildren<Card>();
            card.charIndex = i;
            yield return new WaitForSeconds(dealDelay);

            var enemyVisual = card.cardVisual as CharacterCardVisual;
            var characterModel = enemyVisual.GetComponent<CharacterModel>();
            CharacterCardData characterCardData = GameManager.Instance.GetEnemyData();

            if (characterModel != null)
            {
                characterModel.Initialize(characterCardData); // Reinitialize with new name
            }
            enemyVisual.UpdateSprite();
            enemyVisual.UpdateView();
        }

        enemyHolder.cards = enemyHolder.GetComponentsInChildren<Card>().ToList();

        // Set up event listeners for the new cards
        int cardCount = 0;
        foreach (Card card in enemyHolder.cards)
        {
            // Draw a card from the deck

            card.PointerEnterEvent.AddListener(enemyHolder.CardPointerEnter);
            card.PointerExitEvent.AddListener(enemyHolder.CardPointerExit);
            //card.BeginDragEvent.AddListener(enemyHolder.BeginDrag);
            //card.EndDragEvent.AddListener(enemyHolder.EndDrag);
            card.SelectEvent.AddListener(HandleCardSelection); // Add selection listener
            card.name = cardCount.ToString();

            cardCount++;
        }
        bossTransform = enemyHolder.cards[0].gameObject.transform;


    }

    private IEnumerator DealHeroCoroutine(int numCardDeal)
    {

        // Create slots for the hand
        for (int i = 0; i < numCardDeal; i++)
        {

            // Create a slot and card
            GameObject slot = Instantiate(heroPrefab, heroHolder.transform);
            Card card = slot.GetComponentInChildren<Card>();
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
            card.SelectEvent.AddListener(HandleHeroCardSelection); // Add selection listener
            card.name = cardCount.ToString();
            cardCount++;
        }
        heroTransform = heroHolder.cards[0].gameObject.transform;
        var heroVisual = heroHolder.cards[0].cardVisual as CharacterCardVisual;
        var characterModel = heroVisual.GetComponent<CharacterModel>();

        CharacterCardData characterCardData = GameManager.Instance.GetCharacterCardChosen();

        if (characterModel != null)
        {
            characterModel.Initialize(characterCardData);
        }
        heroVisual.UpdateSprite();
        heroVisual.UpdateView();

        selectedHeroCard = heroHolder.cards[0];
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
        DeSelection();
    }

    public void DeSelection()
    {
        if (handHolder == null || handHolder.cards == null) return;

        foreach (var card in handHolder.cards.ToList())
        {
            if (card.selected)
            {
                card.Deselect();
            }
        }
        selectedCards.Clear();
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

    }
    private IEnumerator DiscardSelectedCardsCoroutineAfterPlayCard()
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

        if (cardsToDiscard.Count == 0)
            yield break;

        foreach (Card card in cardsToDiscard)
        {
            card.transform.DOMove(playcardTransform.position, dealDuration)
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

    private IEnumerator DiscardSelectedCardsCoroutine()
    {
        if (handHolder.cards == null || handHolder.cards.Count == 0)
            yield break;
        if (discardChangeCount <= 0)
        {
            yield break;
        }
        discardChangeCount--;
        discardChangeText.text = discardChangeCount.ToString();
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

        if (cardsToDiscard.Count == 0)
            yield break;

        foreach (Card card in cardsToDiscard)
        {
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



    public void ReshuffleDiscardIntoDeck()
    {
        deckCards.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();

        OnDeckCountChanged?.Invoke(deckCards.Count);
        OnDiscardCountChanged?.Invoke(0);
    }


    public void CalculateScoreWithCombos()
    {
        if (!canPlayCards || selectedCards.Count == 0  /*|| selectedHeroCard == null*/)
            return;

        canPlayCards = false;
        var hand = selectedCards;

        // Check if there are any cards selected
        if (hand.Count == 0)
        {
            Debug.LogWarning("No cards selected for scoring.");
            return;
        }

        int baseScore = 0;
        float comboMultiplier = 1f;

        // Get the actual combo cards
        cardCombo = GetComboCards(hand);

        // Helper: Check for Royal Flush
        bool IsRoyalFlush(List<CardData> cards)
        {
            if (cards.Count != 5) return false;
            var suit = cards[0].suit;
            var ranks = cards.Select(c => c.rank).OrderBy(r => r).ToList();
            return cards.All(c => c.suit == suit) &&
                   ranks.SequenceEqual(new List<CardRank> { CardRank.Ten, CardRank.Jack, CardRank.Queen, CardRank.King, CardRank.Ace });
        }

        // Determine combo type and assign baseScore
        if (IsRoyalFlush(cardCombo))
        {
            baseScore = 2000;
            Debug.Log("Royal Flush");
        }
        else if (cardCombo.Count == 5 && cardCombo.All(c => c.suit == cardCombo[0].suit) &&
                 cardCombo.Select(c => (int)c.rank).OrderBy(x => x).Zip(cardCombo.Select(c => (int)c.rank).OrderBy(x => x).Skip(1), (a, b) => b - a).All(diff => diff == 1))
        {
            baseScore = 600;
            Debug.Log("Straight Flush");
        }
        else if (cardCombo.Count == 4 && cardCombo.GroupBy(c => c.rank).Any(g => g.Count() == 4))
        {
            baseScore = 400;
            Debug.Log("Four of a Kind");
        }
        else if (cardCombo.Count == 5 && cardCombo.GroupBy(c => c.rank).Any(g => g.Count() == 3) && cardCombo.GroupBy(c => c.rank).Any(g => g.Count() == 2))
        {
            baseScore = 175;
            Debug.Log("Full House");
        }
        else if (cardCombo.Count == 5 && cardCombo.All(c => c.suit == cardCombo[0].suit))
        {
            baseScore = 125;
            Debug.Log("Flush");
        }
        else if (cardCombo.Count == 5 && cardCombo.Select(c => (int)c.rank).OrderBy(x => x).Zip(cardCombo.Select(c => (int)c.rank).OrderBy(x => x).Skip(1), (a, b) => b - a).All(diff => diff == 1))
        {
            baseScore = 100;
            Debug.Log("Straight");
        }
        else if (cardCombo.Count == 3 && cardCombo.GroupBy(c => c.rank).Any(g => g.Count() == 3))
        {
            baseScore = 80;
            Debug.Log("Three of a Kind");
        }
        else if (cardCombo.Count == 4 && cardCombo.GroupBy(c => c.rank).Count(g => g.Count() == 2) == 2)
        {
            baseScore = 40;
            Debug.Log("Two Pair");
        }
        else if (cardCombo.Count == 2 && cardCombo.GroupBy(c => c.rank).Any(g => g.Count() == 2))
        {
            baseScore = 20;
            Debug.Log("Pair");
        }
        else
        {
            baseScore = 10;
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

        finalScore = Mathf.RoundToInt((baseScore + CalculateScoreByEachCardRank(cardCombo)) * comboMultiplier * handMultiplier);
        OnScoreCalculated?.Invoke(finalScore, Mathf.RoundToInt(comboMultiplier * handMultiplier));
        Debug.Log($"Final Score: {finalScore} (Base: {baseScore} x Multiplier: {comboMultiplier} x HandMultiplier: {handMultiplier})");

        var heroVisual = heroHolder.cards[0].cardVisual as CharacterCardVisual;

        UpdateScoreText(finalScore + heroVisual.Model.CurrentAttack);

        StartCoroutine(DiscardSelectedCardsCoroutineAfterPlayCard());

        //if (heroHolder == null || heroHolder.cards == null || heroHolder.cards.Count == 0 || bossTransform == null)
        //    return;

    }

    #region Sorting Methods
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
    #endregion

    private void RearrangeCards(List<(Card card, int index, CardData data)> sortedPairs)
    {
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

        DeSelection();
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


    public List<CardData> GetComboCards(List<CardData> hand)
    {
        if (hand == null || hand.Count == 0)
            return new List<CardData>();

        // Helper: Find straight in a list
        List<CardData> FindStraight(List<CardData> cards)
        {
            var sorted = cards.OrderBy(c => (int)c.rank).ToList();
            for (int i = 0; i <= sorted.Count - 5; i++)
            {
                bool isSeq = true;
                for (int j = 0; j < 4; j++)
                {
                    if ((int)sorted[i + j + 1].rank != (int)sorted[i + j].rank + 1)
                    {
                        isSeq = false;
                        break;
                    }
                }
                if (isSeq)
                    return sorted.GetRange(i, 5);
            }
            return new List<CardData>();
        }

        // Helper: Find flush in a list
        List<CardData> FindFlush(List<CardData> cards)
        {
            var suitGroups = cards.GroupBy(c => c.suit).Where(g => g.Count() >= 5).FirstOrDefault();
            return suitGroups != null ? suitGroups.Take(5).ToList() : new List<CardData>();
        }

        // Helper: Find straight flush
        List<CardData> FindStraightFlush(List<CardData> cards)
        {
            var suitGroups = cards.GroupBy(c => c.suit).Where(g => g.Count() >= 5);
            foreach (var group in suitGroups)
            {
                var straightFlush = FindStraight(group.ToList());
                if (straightFlush.Count == 5)
                    return straightFlush;
            }
            return new List<CardData>();
        }

        var rankGroups = hand.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ToList();

        // 1. Straight Flush
        var straightFlush = FindStraightFlush(hand);
        if (straightFlush.Count == 5)
            return straightFlush;

        // 2. Four of a Kind
        var four = rankGroups.FirstOrDefault(g => g.Count() == 4);
        if (four != null)
            return four.ToList();

        // 3. Full House
        var three = rankGroups.FirstOrDefault(g => g.Count() == 3);
        var pair = rankGroups.Where(g => g.Count() >= 2 && g.Key != (three != null ? three.Key : CardRank.Two - 1)).FirstOrDefault();
        if (three != null && pair != null)
            return three.Take(3).Concat(pair.Take(2)).ToList();

        // 4. Flush
        var flush = FindFlush(hand);
        if (flush.Count == 5)
            return flush;

        // 5. Straight
        var straight = FindStraight(hand);
        if (straight.Count == 5)
            return straight;

        // 6. Three of a Kind
        if (three != null)
            return three.ToList();

        // 7. Two Pair
        var pairs = rankGroups.Where(g => g.Count() == 2).Take(2).ToList();
        if (pairs.Count == 2)
            return pairs[0].ToList().Concat(pairs[1].ToList()).ToList();

        // 8. Pair
        if (pairs.Count == 1)
            return pairs[0].ToList();

        // 9. High Card (return the highest card)
        return new List<CardData> { hand.OrderByDescending(c => (int)c.rank).First() };
    }

    private int CalculateScoreByEachCardRank(List<CardData> cardDatas)
    {
        int current = 0;
        foreach (var cardData in cardDatas)
        {
            int addValue = 0;
            if (cardData.rank == CardRank.Ace)
            {
                addValue = 15;
            }
            else if (cardData.rank == CardRank.King || cardData.rank == CardRank.Queen || cardData.rank == CardRank.Jack)
            {
                addValue = 12;
            }
            else
            {
                addValue = (int)cardData.rank;
            }
            current += addValue;
        }
        return current;
    }

    #region ScoreText
    private IEnumerator AddScoreByCardRank(CardData card)
    {
        if (scoreText == null || card == null)
            yield break;

        int current = 0;
        int.TryParse(scoreText.text, out current);

        int addValue = 0;
        if (card.rank == CardRank.Ace)
        {
            addValue = 15;
        }
        else if (card.rank == CardRank.King || card.rank == CardRank.Queen || card.rank == CardRank.Jack)
        {
            addValue = 12;
        }
        else
        {
            addValue = (int)card.rank;
        }

        int newValue = current + addValue;
        scoreText.text = newValue.ToString();

        // Optional: pop animation
        scoreText.DOKill();
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform.DOScale(1.6f, 0.1f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => scoreText.transform.DOScale(1f, 0.1f).SetEase(Ease.InBack));

        yield return new WaitForSeconds(dealDelay);

    }
    private void UpdateScoreText(int value)
    {
        if (scoreText == null)
            return;
        int oldValue = int.Parse(scoreText.text);
        scoreText.text = (oldValue + value).ToString();
        scoreText.DOKill(); // Stop any previous tweens
        scoreText.transform.localScale = Vector3.one;
        scoreText.color = new Color(scoreText.color.r, scoreText.color.g, scoreText.color.b, 1f); // Ensure visible
        scoreText.transform.DOScale(1.6f, 0.1f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => scoreText.transform.DOScale(1f, 0.1f).SetEase(Ease.InBack));
    }
    public void FadeOutScoreText()
    {
        if (scoreText == null)
            return;

        scoreText.DOFade(0f, 0.3f).SetEase(Ease.InOutQuad);
        scoreText.text = "0";
    }
    #endregion


}