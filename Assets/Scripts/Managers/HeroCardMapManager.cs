using DG.Tweening;
using Map;
using System.Collections;
using UnityEngine;
public class HeroCardMapManager : MonoBehaviour
{
    [Header("Card Settings")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private HorizontalCardHolder cardHolder;

    [Header("Animation draw Settings")]
    [SerializeField] private float dealDuration = 0.5f;
    [SerializeField] private float scaleUpDuration = 0.3f;
    [SerializeField] private Ease dealEase = Ease.OutBack;

    [Header("Position Settings")]
    [SerializeField] private Vector3 dealFromPosition;
    [SerializeField] private Vector3 finalScale = Vector3.one;

    [SerializeField] private Card currentCard;
    [SerializeField] private CharacterModel cardData;
    [SerializeField] private CharacterCardVisual cardVisual;

    private void Start()
    {
        // Initialize cardHolder if needed
        if (cardHolder == null)
        {
            cardHolder = GetComponentInChildren<HorizontalCardHolder>();
        }

        // If there's hero data in GameSession, draw the card
        if (GameManager.Instance.GetCharacterCardChosen() != null)
        {
            StartCoroutine(DrawHeroCard());
        }
    }

    public IEnumerator DrawHeroCard()
    {
        // Clear any existing card and data
        if (currentCard != null)
        {
            Destroy(currentCard);
        }
        if (cardData != null)
        {
            cardData = null;
        }

        // Create new card
        currentCard = Instantiate(heroPrefab, cardHolder.transform).GetComponentInChildren<Card>();
        yield return new WaitForSeconds(0.1f);

        cardVisual = currentCard.cardVisual as CharacterCardVisual;
        cardData = cardVisual.GetComponent<CharacterModel>();

        currentCard.PointerEnterEvent.AddListener(cardHolder.CardPointerEnter);
        currentCard.PointerExitEvent.AddListener(cardHolder.CardPointerExit);
        currentCard.BeginDragEvent.AddListener(cardHolder.BeginDrag);
        currentCard.EndDragEvent.AddListener(cardHolder.EndDrag);


        CharacterCardData characterCardData = GameManager.Instance.GetCharacterCardChosen();

        if (cardData != null)
        {
            cardData.Initialize(characterCardData);
        }
        cardVisual.UpdateSprite();
        cardVisual.UpdateView();

    }
    public void HeroesRest()
    {
        if (currentCard == null)
        {
            return;
        }
        int maxHealth = cardData.BaseHealth;
        int curHealth = cardData.CurrentHealth;
        if (curHealth < maxHealth)
        {
            int healthAmount = (int)((maxHealth - curHealth) * 0.8f);
            cardVisual.PlayHealAnimation(healthAmount);
            GameManager.Instance.SetCharacterCardChosen(cardData);
        }
        var tracker = FindObjectOfType<MapPlayerTracker>();
        if (tracker != null)
        {
            tracker.Locked = false;
        }
    }
    private void OnDestroy()
    {
        // Cleanup
        if (cardData != null)
        {
            cardData = null;
        }
        if (currentCard != null)
        {
            currentCard.transform.DOKill();
            Destroy(currentCard);
        }
    }
}