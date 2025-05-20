using DG.Tweening;
using Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HeroCardMapManager : MonoBehaviour
{
    [Header("Card Settings")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private HeroHorrizontalHolder cardHolder;

    [Header("Animation draw Settings")]
    [SerializeField] private float dealDuration = 0.5f;
    [SerializeField] private float scaleUpDuration = 0.3f;
    [SerializeField] private Ease dealEase = Ease.OutBack;

    [Header("Position Settings")]
    [SerializeField] private Vector3 dealFromPosition;
    [SerializeField] private Vector3 finalScale = Vector3.one;

    private HeroCardScriptable tempHeroCard;
    [SerializeField] private GameObject currentCard;
    [SerializeField] private HeroSelectData currentSelectData;
    [SerializeField] private HeroCardSelector cardVisual;

    private void Start()
    {
        // Initialize cardHolder if needed
        if (cardHolder == null)
        {
            cardHolder = GetComponentInChildren<HeroHorrizontalHolder>();
        }
        
        // Set card holder to only spawn 1 card
        if (cardHolder != null)
        {
            cardHolder.cardsToSpawn = 1;
        }

        // If there's hero data in GameSession, draw the card
        if (GameSession.heroes != null)
        {
            DrawHeroCard();
        }
    }

    public void DrawHeroCard()
    {
        // Clear any existing card and data
        if (currentCard != null)
        {
            Destroy(currentCard);
        }
        if (tempHeroCard != null)
        {
            Destroy(tempHeroCard);
        }

        // Create hero data
        tempHeroCard = CreateHeroCardData();

        // Create new card
        currentCard = Instantiate(heroPrefab, dealFromPosition, Quaternion.identity, cardHolder.transform);
        currentCard.transform.localScale = Vector3.zero;

        // Get and setup components
        currentSelectData = currentCard.GetComponentInChildren<HeroSelectData>();
        currentSelectData.heroData = CreateHeroCardData();
        cardVisual = currentSelectData.heroVisual;
    }
    
    private HeroCardScriptable CreateHeroCardData()
    {
        // Create a temporary ScriptableObject
        var heroCard = ScriptableObject.CreateInstance<HeroCardScriptable>();
        
        // Copy data from GameSession.heroes
        if (GameSession.heroes != null)
        {
            heroCard.id = GameSession.heroes.id;
            heroCard.Name = GameSession.heroes.Name;
            heroCard.Description = GameSession.heroes.Description;
            heroCard.Sprite = GameSession.heroes.Sprite;
            heroCard.maxHealth = GameSession.heroes.maxHealth;
            heroCard.currentHealth = GameSession.heroes.currentHealth;
            heroCard.attack = GameSession.heroes.attack;
            heroCard.defense = GameSession.heroes.defense;
            heroCard.isUnlocked = true;
        }

        return heroCard;
    }
    public void HealHero()
    {
        if (currentCard == null)
        {
            return;
        }

        // Update hero data first
        tempHeroCard = CreateHeroCardData();  // Get fresh data with new health value
        currentSelectData.heroData = tempHeroCard;
        currentSelectData.HealHero();
        currentSelectData.ReloadHero();
    }
    private void OnDestroy()
    {
        // Cleanup
        if (tempHeroCard != null)
        {
            Destroy(tempHeroCard);
        }
        if (currentCard != null)
        {
            currentCard.transform.DOKill();
            Destroy(currentCard);
        }
    }
}