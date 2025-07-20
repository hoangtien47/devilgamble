using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public DeckManager deckManager;
    public HorizontalCardHolder heroHolder;
    public HorizontalCardHolder enemyHolder;

    private class TurnEntry
    {
        public CharacterCardVisual visual;
        public float nextTurnTime;

        public TurnEntry(CharacterCardVisual visual, float currentTime)
        {
            this.visual = visual;
            this.nextTurnTime = currentTime + GetTimeToNextTurn(visual);
        }

        public static float GetTimeToNextTurn(CharacterCardVisual v)
        {
            // 1000 is arbitrary "initiative fill" value like Honkai: Star Rail
            return 1000f / Mathf.Max(1, v.Model.CurrentSpeed);
        }
    }

    private List<TurnEntry> timeline = new List<TurnEntry>();
    private float currentTime = 0f;
    private bool isProcessingTurn = false;

    public static event Action<bool, bool> OnGameOver; // true = win, false = lose


    private void Start()
    {
        StartCoroutine(WaitForInitialCards());
    }

    private IEnumerator WaitForInitialCards()
    {
        yield return new WaitUntil(() => heroHolder.cards.Count > 0 && enemyHolder.cards.Count > 0);
        InitializeTimeline();
        StartCoroutine(TurnLoop());
    }

    private void InitializeTimeline()
    {
        currentTime = 0f;
        timeline.Clear();

        var allCards = heroHolder.cards.Concat(enemyHolder.cards);
        foreach (var card in allCards)
        {
            var visual = card.cardVisual as CharacterCardVisual;
            if (visual != null && visual.Model.IsAlive)
            {
                timeline.Add(new TurnEntry(visual, currentTime));
            }
        }

        SortTimeline();
    }

    private void SortTimeline()
    {
        timeline = timeline.OrderBy(t => t.nextTurnTime).ToList();
    }

    private IEnumerator TurnLoop()
    {
        while (true)
        {
            if (timeline.Count == 0)
            {
                Debug.Log("No more characters to take turns.");
                yield break;
            }

            timeline.RemoveAll(entry => !entry.visual.Model.IsAlive);

            // Check victory condition
            bool allEnemiesDead = enemyHolder.cards.All(c => !((CharacterCardVisual)c.cardVisual).Model.IsAlive);
            bool allHeroesDead = heroHolder.cards.All(c => !((CharacterCardVisual)c.cardVisual).Model.IsAlive);

            if (allEnemiesDead || allHeroesDead)
            {
                var heroVisual = heroHolder.cards[0].cardVisual as CharacterCardVisual;

                Debug.Log(heroVisual.Model.CurrentHealth + " has been chosen as the hero.");

                GameManager.Instance.SetCharacterCardChosen(heroVisual.Model);

                Debug.Log(GameManager.Instance.GetCharacterCardChosen().currentHealth);

                bool isBoss = enemyHolder.cards.Any(c => ((CharacterCardVisual)c.cardVisual).Model.Rarity == Rarity.Boss);

                if (isBoss && allEnemiesDead)
                {
                    bool isComplete = GameManager.Instance.SetCurrentLevelStatus(allEnemiesDead);
                    OnGameOver?.Invoke(true, isComplete);
                    yield break;
                }

                OnGameOver?.Invoke(allEnemiesDead, false);

                yield break;
            }

            var next = timeline[0];

            // Fast-forward current time
            currentTime = next.nextTurnTime;

            // Remove from queue
            timeline.RemoveAt(0);

            if (!next.visual.Model.IsAlive)
            {
                continue;
            }

            yield return StartCoroutine(HandleTurn(next.visual));

            // Re-insert this character into the timeline
            timeline.Add(new TurnEntry(next.visual, currentTime));
            SortTimeline();
        }
    }

    private IEnumerator HandleTurn(CharacterCardVisual actor)
    {
        isProcessingTurn = true;

        if (actor.Model.Team == CharacterTeam.Hero)
        {
            Debug.Log($"Player Turn: {actor.Model.CharacterName} (Speed: {actor.Model.CurrentSpeed})");

            deckManager.canPlayCards = true;

            // Wait until player finishes turn
            yield return new WaitUntil(() => deckManager.canPlayCards == false);

            yield return new WaitForSeconds(1f);

            var target = enemyHolder.cards.FirstOrDefault()?.cardVisual as CharacterCardVisual;
            if (target != null && target.Model.IsAlive)
            {
                actor.AttackCharacter(target, deckManager.finalScore);
            }

            yield return new WaitForSeconds(1f);
            deckManager.finalScore = 0;
            deckManager.FadeOutScoreText();
        }
        else
        {
            Debug.Log($"Enemy Turn: {actor.Model.CharacterName} (Speed: {actor.Model.CurrentSpeed})");

            yield return new WaitForSeconds(1f);

            var target = heroHolder.cards.FirstOrDefault()?.cardVisual as CharacterCardVisual;
            if (target != null && target.Model.IsAlive)
            {
                actor.AttackCharacter(target, 0);
            }

            yield return new WaitForSeconds(1f);
        }

        isProcessingTurn = false;
    }
}
