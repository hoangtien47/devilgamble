using DG.Tweening;
using TMPro;
using UnityEngine;
public class CharacterCardVisual : CardVisual
{
    [Header("==========UI Character==========")]
    [SerializeField] private TextMeshProUGUI _HPText;
    [SerializeField] private TextMeshProUGUI _ATKText;
    [SerializeField] private TextMeshProUGUI _NameText;

    [Header("Attack Animation")]
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private Ease attackEase = Ease.OutQuint;
    [SerializeField] private Ease returnEase = Ease.OutBack;
    [SerializeField] private float attackDistance = 0.7f; // How close to get to the boss (0-1)

    public override void Initialize(Card target)
    {
        // Call base implementation first to set up common functionality and event listeners
        base.Initialize(target);

        //cardImage.sprite = characterCardScriptable.Sprite;
    }

    public void OnChangeData(int HP, int ATK)
    {
        _HPText.SetText(HP.ToString());
        _ATKText.SetText(ATK.ToString());
    }
    public void OnLoadCharacter(BaseCharacter character)
    {
        if (character == null) return;
        _HPText.SetText(character.HP.ToString());
        _ATKText.SetText(character.ATK.ToString());
        _NameText.SetText(character.Name);
        cardImage.sprite = character.Sprite;
    }

    public Tween Attack(Transform targetTransform, System.Action onHitCallback = null)
    {
        if (isBeingDestroyed || targetTransform == null || transform == null || shakeParent == null)
            return null;

        DOTween.Kill(transform);

        Vector3 originalPosition = transform.position;

        // Calculate attack position
        Vector3 attackPosition = Vector3.Lerp(
            originalPosition,
            targetTransform.position,
            attackDistance
        );

        Sequence attackSequence = DOTween.Sequence();
        attackSequence.SetLink(gameObject);

        // Step 1: Pick up (move upward slightly)
        attackSequence.Append(transform.DOMoveY(originalPosition.y + 0.5f, 0.2f).SetEase(Ease.OutBack));

        // Step 2: Aggressively move toward the target
        attackSequence.Append(transform.DOMove(attackPosition, attackDuration * 0.8f).SetEase(Ease.InQuad));

        // Step 3: Apply knockback and shake to the target
        attackSequence.AppendCallback(() =>
        {
            if (isBeingDestroyed || shakeParent == null) return;

            // Knockback effect on the target
            targetTransform.DOMove(targetTransform.position + (targetTransform.position - transform.position).normalized * 0.5f, 0.2f)
                .SetEase(Ease.OutQuad);

            // Shake effect on the target
            targetTransform.DOShakePosition(0.3f, strength: 0.3f, vibrato: 10, randomness: 90);

            // Trigger the hit callback
            onHitCallback?.Invoke();
        });

        // Step 4: Return to the original position
        attackSequence.Append(transform.DOMove(originalPosition, returnDuration).SetEase(returnEase));

        // Step 5: Reset rotation and scale
        attackSequence.Join(shakeParent.DORotate(Vector3.zero, returnDuration, RotateMode.Fast));
        attackSequence.Join(transform.DOScale(1f, returnDuration).SetEase(Ease.OutBack));

        return attackSequence;
    }


    public Tween AttackedEffect(float intensity = 1.0f, System.Action onCompleteCallback = null)
    {
        if (isBeingDestroyed || shakeParent == null)
            return null;

        DOTween.Kill(transform);

        // Store original values
        Vector3 originalPosition = transform.position;
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;

        Sequence attackedSequence = DOTween.Sequence();
        attackedSequence.SetLink(gameObject);

        // Flash red effect (requires cardImage to be accessible)
        if (cardImage != null)
        {
            Color originalColor = cardImage.color;
            attackedSequence.Append(cardImage.DOColor(Color.red, 0.1f));
            attackedSequence.Append(cardImage.DOColor(originalColor, 0.2f));
        }

        // Shake effect
        attackedSequence.Join(shakeParent.DOPunchRotation(
            new Vector3(intensity * 10f, intensity * 5f, intensity * 15f),
            0.3f,
            10,
            0.5f
        ));

        // Vibration effect
        attackedSequence.Join(transform.DOShakePosition(
            0.4f,
            strength: new Vector3(0.2f, 0.2f, 0) * intensity,
            vibrato: 20,
            randomness: 90,
            snapping: false,
            fadeOut: true
        ));

        // Scale punch for impact feeling
        attackedSequence.Join(transform.DOPunchScale(
            new Vector3(-0.3f, -0.3f, 0) * intensity,
            0.3f,
            10,
            0.5f
        ));

        // Ensure we return to original state
        attackedSequence.OnComplete(() =>
        {
            if (isBeingDestroyed) return;

            // Make sure we're back to original state
            if (transform != null)
            {
                transform.position = originalPosition;
                transform.localScale = originalScale;
                transform.rotation = originalRotation;
            }

            onCompleteCallback?.Invoke();
        });

        return attackedSequence;
    }
    public void PlayExplosionEffect()
    {
        if (isBeingDestroyed) return;
        isBeingDestroyed = true;

        // Optional: randomize direction for a more dynamic effect
        Vector2 randomDir = Random.insideUnitCircle.normalized * 80f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.7f, 0.18f).SetEase(Ease.OutBack));
        seq.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-180f, 180f)), 0.18f, RotateMode.FastBeyond360));
        seq.Join(transform.DOMove(transform.position + (Vector3)randomDir, 0.18f).SetEase(Ease.OutQuad));
        if (cardImage != null)
            seq.Join(cardImage.DOFade(0f, 0.18f));
        seq.AppendCallback(() => Destroy(gameObject));
    }

    public void LoadCharacterData(EnemyCardData enemyCardData)
    {
        var enemyParentCard = parentCard as CharacterCard;
        enemyParentCard.LoadEnemyData(enemyCardData);
    }
    public void LoadCharacterData(HeroCardData heroData)
    {
        var heroParentCard = parentCard as CharacterCard;

        heroParentCard.LoadHeroData(heroData);
    }
}
