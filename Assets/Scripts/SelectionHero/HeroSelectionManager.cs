using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelectionManager : MonoBehaviour
{
    [Header("Hero Card")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private HeroHorrizontalHolder heroHolder;
    private HeroSelectData selectedHeroCard = null;

    [Header("Hero Selection")]
    [SerializeField] private Transform heroSelectedTransform;
    [SerializeField] private HeroHorrizontalHolder selectedHolder; // Holder for selected card
    [SerializeField] private float moveToSelectedDuration = 0.5f;
    [SerializeField] private Ease moveToSelectedEase = Ease.OutBack;

    [Header("Hero Data")]
    [SerializeField] private List<HeroCardScriptable> heroData;

    [Header("Deck Settings")]
    [SerializeField] private float dealDelay = 0.1f;
    [SerializeField] private float dealDuration = 0.3f;
    [SerializeField] private Ease dealEase = Ease.OutBack;

    [Header("UI")]
    [SerializeField] private Button loadMapButton;
    void Start()
    {
        if(heroData!=null && heroData.Count > 0)
        {
            StartCoroutine(DealHeroCoroutine(heroData.Count));
        }

        // Initialize selected holder
        selectedHolder = heroSelectedTransform.GetComponent<HeroHorrizontalHolder>();
        if (selectedHolder == null)
        {
            selectedHolder = heroSelectedTransform.gameObject.AddComponent<HeroHorrizontalHolder>();
        }
        // Initialize selected holder properties
        selectedHolder.cards = new List<HeroSelectData>();
        selectedHolder.cardsToSpawn = 1;

        // Initialize load map button
        if (loadMapButton != null)
        {
            loadMapButton.onClick.AddListener(LoadMap);
            loadMapButton.interactable = false; // Start with button disabled
        }
    }

    private IEnumerator DealHeroCoroutine(int numCardDeal)
    {

        // Create slots for the hand
        for (int i = 0; i < numCardDeal; i++)
        {

            // Create a slot and card
            GameObject slot = Instantiate(heroPrefab, heroHolder.transform);
            HeroSelectData card = slot.GetComponentInChildren<HeroSelectData>();
            card.heroData = heroData[i];
            card.idx = i;
            yield return new WaitForSeconds(dealDelay);
        }

        // Update the card holder
        heroHolder.cards = heroHolder.GetComponentsInChildren<HeroSelectData>().ToList();

        // Set up event listeners for the new cards
        int cardCount = 0;
        foreach (HeroSelectData card in heroHolder.cards)
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
    }
    public void HandleHeroCardSelection(HeroSelectData card, bool isSelected)
    {
        if (!card.heroData.isUnlocked) return;

        if (isSelected)
        {
            if (selectedHeroCard != null)
            {
                if (selectedHeroCard != card)
                {
                    // Move previous card back to main holder
                    MoveCardBetweenHolders(selectedHeroCard, selectedHolder, heroHolder);
                    selectedHeroCard.Deselect();

                    // Move new card to selected holder and update GameSession
                    MoveCardBetweenHolders(card, heroHolder, selectedHolder);
                    selectedHeroCard = card;
                    UpdateGameSession(card.heroData);
                }
                else
                {
                    // Same card selected again, move it back
                    MoveCardBetweenHolders(card, selectedHolder, heroHolder);
                    selectedHeroCard = null;
                    ClearGameSession();
                }
            }
            else
            {
                // No previous selection, move card to selected holder
                MoveCardBetweenHolders(card, heroHolder, selectedHolder);
                selectedHeroCard = card;
                UpdateGameSession(card.heroData);
            }
        }
        else
        {
            if (selectedHeroCard == card)
            {
                MoveCardBetweenHolders(card, selectedHolder, heroHolder);
                selectedHeroCard = null;
                ClearGameSession();
            }
        }
    }
    private void MoveCardBetweenHolders(HeroSelectData card, HeroHorrizontalHolder fromHolder, HeroHorrizontalHolder toHolder)
    {
        if (card == null || card.heroVisual == null) return;

        // Check if there's already a card in the target holder (for selectedHolder)
        if (toHolder.cards.Count > 0 && toHolder == selectedHolder)
        {
            // Move existing card back to main holder first
            var existingCard = toHolder.cards[0]; // Should only be one card in selectedHolder
            MoveCardBetweenHolders(existingCard, toHolder, heroHolder);
            existingCard.Deselect();
        }

        // Remove from current holder
        fromHolder.cards.Remove(card);

        // Add to new holder
        toHolder.cards.Add(card);

        // Store the parent GameObject (slot)
        GameObject cardSlot = card.transform.parent.gameObject;

        // Create sequence for smooth animation
        Sequence sequence = DOTween.Sequence();

        // Pop effect
        sequence.Append(card.heroVisual.transform
            .DOScale(Vector3.one * 1.2f, moveToSelectedDuration * 0.3f)
            .SetEase(Ease.OutBack));

        // Move the parent slot to new position
        sequence.Append(cardSlot.transform
            .DOMove(toHolder.transform.position, moveToSelectedDuration)
            .SetEase(moveToSelectedEase));

        // Return to normal scale
        sequence.Append(card.heroVisual.transform
            .DOScale(Vector3.one, moveToSelectedDuration * 0.3f)
            .SetEase(Ease.OutBack));

        // Change parent after animation
        sequence.OnComplete(() =>
        {
            // Move the entire slot (parent) to the new holder
            cardSlot.transform.SetParent(toHolder.transform);

            // Reset local position of the slot
            cardSlot.transform.localPosition = Vector3.zero;

            // Update indices in both holders
            fromHolder.UpdateCardVisuals();
            toHolder.UpdateCardVisuals();
        });
    }
    private void UpdateGameSession(HeroCardScriptable heroData)
    {
        // Store the selected hero data in GameSession
        GameSession.heroes = heroData;
        UpdateLoadMapButton();
    }

    private void ClearGameSession()
    {
        // Clear the hero data when deselected
        GameSession.heroes = null;
        UpdateLoadMapButton();
    }

    // Update button state when hero is selected
    private void UpdateLoadMapButton()
    {
        if (loadMapButton != null)
        {
            loadMapButton.interactable = selectedHeroCard != null;
        }
    }

    public void LoadMap()
    {
        // Check if we have a selected hero
        if (selectedHeroCard == null || GameSession.heroes == null)
        {
            Debug.LogWarning("Cannot load map: No hero selected!");
            return;
        }

        // Save the selected hero to GameSession (already done in UpdateGameSession)

        // Load the map scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(2); // Replace "Map" with your actual map scene name
    }
}
