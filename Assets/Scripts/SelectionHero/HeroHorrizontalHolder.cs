using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HeroHorrizontalHolder : MonoBehaviour
{
    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] public int cardsToSpawn = 7;
    public List<Card> cards;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;

    [Header("Swap Animation")]
    [SerializeField] private float swapDuration = 0.1f;  // Faster swap duration
    [SerializeField] private float swapScale = 0.95f;    // Less scale change for quicker recovery
    [SerializeField] private Ease swapEase = Ease.OutQuad;

    bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void DrawCard()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cards.Clear();

        for (int i = 0; i < cardsToSpawn; i++)
        {
            GameObject slot = Instantiate(slotPrefab, transform);
            var heroData = slot.GetComponent<Card>();
            if (heroData != null)
            {
                cards.Add(heroData);

                // Setup event listeners
                heroData.name = $"HeroCard_{i}";
            }
        }

        // Sort cards initially
        SortCardsByID();

        StartCoroutine(UpdateVisuals());
    }

    public void BeginDrag(Card card)
    {
        selectedCard = card;
    }

    public void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        // Return card to its position in the holder
        selectedCard.transform.DOLocalMove(
            selectedCard.selected ? new Vector3(0, selectedCard.selectionOffset, 0) : Vector3.zero,
            tweenCardReturn ? .15f : 0
        ).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        // If no hover, show selected card tooltip
        if (hoveredCard == null && selectedCard != null)
        {
            var visual = card.cardVisual as CharacterCardVisual;
            TooltipSelectedHeroes.Instance.ShowTooltip(visual.Model);
        }

        selectedCard = null;
    }

    public void CardPointerEnter(Card card)
    {
        if (card == null || card.cardVisual == null) return;

        hoveredCard = card;

        // Show tooltip for hovered card
        if (TooltipSelectedHeroes.Instance != null)
        {
            var visual = card.cardVisual as CharacterCardVisual;
            TooltipSelectedHeroes.Instance.ShowTooltip(visual.Model);
        }
    }

    public void CardPointerExit(Card card)
    {
        if (card == null || card.cardVisual == null) return;

        hoveredCard = null;
        // If there's a selected card, show its tooltip instead
        if (TooltipSelectedHeroes.Instance != null && selectedCard != null)
        {
            var visual = card.cardVisual as CharacterCardVisual;
            TooltipSelectedHeroes.Instance.ShowTooltip(visual.Model);
        }
        else if (TooltipSelectedHeroes.Instance != null)
        {
            TooltipSelectedHeroes.Instance.HideTooltip();
        }
    }
    private IEnumerator UpdateVisuals()
    {
        yield return new WaitForEndOfFrame();
        foreach (var card in cards)
        {
            if (card.cardVisual != null)
            {
                card.cardVisual.UpdateIndex(transform.childCount);
                // Ensure initial scale is correct
                card.cardVisual.transform.localScale = Vector3.one;
            }
        }
    }
    public void UpdateCardVisuals()
    {
        // Update card positions and sorting
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].cardVisual != null)
            {
                cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
        SortCardsByID();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);

            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

    }
    private void SortCardsByID()
    {
        // Sort the cards list by hero ID, handling string IDs like "E1", "E2", "E3"
        cards = cards.OrderBy(card =>
        {
            var visual = card.cardVisual as CharacterCardVisual;
            string id = visual.GetComponent<CharacterModel>().CharacterName;
            return id;
        }).ToList();

        // Reposition cards based on new order
        for (int i = 0; i < cards.Count; i++)
        {

            // Update card parent and position
            Transform targetSlot = transform.GetChild(i);
            cards[i].transform.SetParent(targetSlot);
            cards[i].transform.localPosition = cards[i].selected ?
                new Vector3(0, cards[i].selectionOffset, 0) : Vector3.zero;

            // Update visual
            if (cards[i].cardVisual != null)
            {
                cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }

        // Force layout update if needed
        if (TryGetComponent<HorizontalLayoutGroup>(out var layout))
        {
            layout.enabled = false;
            layout.enabled = true;
        }
    }
    public void TransferCardTo(Card card, Transform newParent)
    {
        if (card == null || !cards.Contains(card)) return;

        // Remove from current holder
        cards.Remove(card);

        // Change parent
        card.transform.SetParent(newParent);

        // Update visuals
        UpdateCardVisuals();
    }
    public void ReceiveCard(Card card)
    {
        if (card == null) return;

        // Add to this holder's list
        if (!cards.Contains(card))
        {
            cards.Add(card);
            card.transform.SetParent(transform);
        }

        // Update visuals
        UpdateCardVisuals();
    }
}
