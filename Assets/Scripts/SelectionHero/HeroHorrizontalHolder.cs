using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HeroHorrizontalHolder : MonoBehaviour
{
    [SerializeField] private HeroSelectData selectedCard;
    [SerializeReference] private HeroSelectData hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] public int cardsToSpawn = 7;
    public List<HeroSelectData> cards;

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
            var heroData = slot.GetComponent<HeroSelectData>();
            if (heroData != null)
            {
                heroData.idx = i;
                cards.Add(heroData);

                // Setup event listeners
                heroData.PointerEnterEvent.AddListener(CardPointerEnter);
                heroData.PointerExitEvent.AddListener(CardPointerExit);
                heroData.BeginDragEvent.AddListener(BeginDrag);
                heroData.EndDragEvent.AddListener(EndDrag);
                heroData.name = $"HeroCard_{i}";
            }
        }

        // Sort cards initially
        SortCardsByID();

        StartCoroutine(UpdateVisuals());
    }

    public void BeginDrag(HeroSelectData card)
    {
        selectedCard = card;
    }

    public void EndDrag(HeroSelectData card)
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
            TooltipSelectedHeroes.Instance.ShowTooltip(selectedCard.heroData);
        }

        selectedCard = null;
        UpdateCardVisuals();
    }

    public void CardPointerEnter(HeroSelectData card)
    {
        if (card == null || card.heroVisual == null) return;

        hoveredCard = card;

        // Kill any existing tweens on the visual
        DOTween.Kill(card.heroVisual.transform);

        // Scale up animation
        card.heroVisual.transform
            .DOScale(Vector3.one * hoverScale, hoverDuration)
            .SetEase(hoverEase);

        // Show tooltip for hovered card
        if (TooltipSelectedHeroes.Instance != null)
        {
            TooltipSelectedHeroes.Instance.ShowTooltip(card.heroData);
        }
    }

    public void CardPointerExit(HeroSelectData card)
    {
        if (card == null || card.heroVisual == null) return;

        hoveredCard = null;

        // Kill any existing tweens on the visual
        DOTween.Kill(card.heroVisual.transform);

        // Scale down animation
        card.heroVisual.transform
            .DOScale(Vector3.one, hoverDuration)
            .SetEase(hoverEase);

        // If there's a selected card, show its tooltip instead
        if (TooltipSelectedHeroes.Instance != null && selectedCard != null)
        {
            TooltipSelectedHeroes.Instance.ShowTooltip(selectedCard.heroData);
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
            if (card.heroVisual != null)
            {
                card.heroVisual.UpdateIndex(transform.childCount);
                // Ensure initial scale is correct
                card.heroVisual.transform.localScale = Vector3.one;
            }
        }
    }
    public void UpdateCardVisuals()
    {
        // Update card positions and sorting
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].heroVisual != null)
            {
                cards[i].idx = i;
                cards[i].heroVisual.UpdateIndex(transform.childCount);
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
            foreach (HeroSelectData card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {

            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        if (isCrossing) return;
        isCrossing = true;

        // Store transforms
        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        // Create swap sequence for both cards
        Sequence swapSequence = DOTween.Sequence();

        // Quick scale down for swap preparation
        swapSequence.Append(selectedCard.heroVisual.transform
            .DOScale(Vector3.one * 0.95f, 0.1f)
            .SetEase(Ease.OutQuad));

        // Quick parent switch and position update
        swapSequence.AppendCallback(() =>
        {
            cards[index].transform.SetParent(focusedParent);
            selectedCard.transform.SetParent(crossedParent);

            // Update positions immediately
            cards[index].transform.localPosition = cards[index].selected ?
                new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
            selectedCard.transform.localPosition = selectedCard.selected ?
                new Vector3(0, selectedCard.selectionOffset, 0) : Vector3.zero;
        });

        // Quick scale restore
        swapSequence.Append(selectedCard.heroVisual.transform
            .DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.OutBack));

        // Quick swap animation
        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        if (cards[index].heroVisual != null)
        {
            // Faster rotation punch
            cards[index].heroVisual.Swap(swapIsRight ? -1 : 1);
            selectedCard.heroVisual?.Swap(swapIsRight ? 1 : -1);
        }

        // Complete sequence
        swapSequence.OnComplete(() =>
        {
            isCrossing = false;
            SortCardsByID();
        });

        // Make sequence faster
        swapSequence.SetUpdate(true);  // Update even during timeScale changes
        swapSequence.timeScale = 1.5f; // Make it 50% faster
    }

    private void SortCardsByID()
    {
        // Sort the cards list by hero ID, handling string IDs like "E1", "E2", "E3"
        cards = cards.OrderBy(card =>
        {
            string id = card.heroData.id;
            string numberPart = new string(id.Where(char.IsDigit).ToArray());
            return int.Parse(numberPart);
        }).ToList();

        // Reposition cards based on new order
        for (int i = 0; i < cards.Count; i++)
        {
            // Update card index
            cards[i].idx = i;

            // Update card parent and position
            Transform targetSlot = transform.GetChild(i);
            cards[i].transform.SetParent(targetSlot);
            cards[i].transform.localPosition = cards[i].selected ?
                new Vector3(0, cards[i].selectionOffset, 0) : Vector3.zero;

            // Update visual
            if (cards[i].heroVisual != null)
            {
                cards[i].heroVisual.UpdateIndex(transform.childCount);
            }
        }

        // Force layout update if needed
        if (TryGetComponent<HorizontalLayoutGroup>(out var layout))
        {
            layout.enabled = false;
            layout.enabled = true;
        }
    }
    public void TransferCardTo(HeroSelectData card, Transform newParent)
    {
        if (card == null || !cards.Contains(card)) return;

        // Remove from current holder
        cards.Remove(card);

        // Change parent
        card.transform.SetParent(newParent);

        // Update visuals
        UpdateCardVisuals();
    }
    public void ReceiveCard(HeroSelectData card)
    {
        if (card == null) return;

        // Add to this holder's list
        if (!cards.Contains(card))
        {
            cards.Add(card);
            card.transform.SetParent(transform);

            // Update card's position in the holder
            card.idx = cards.Count - 1;
        }

        // Update visuals
        UpdateCardVisuals();
    }
}
