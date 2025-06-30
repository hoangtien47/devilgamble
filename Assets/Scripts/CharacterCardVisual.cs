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
        {
            return null;
        }

        // Kill any existing tweens
        DOTween.Kill(transform);

        // IMPORTANT: Store by value, not by reference
        Vector3 originalPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        // Calculate attack position
        Vector3 attackPosition = Vector3.Lerp(
            originalPosition,
            targetTransform.position,
            attackDistance
        );

        Sequence attackSequence = DOTween.Sequence();
        attackSequence.SetId("AttackSequence");
        attackSequence.SetLink(gameObject); // Auto-kill if gameObject is destroyed

        // Store a local reference to targetTransform to use in a safe way
        Transform targetRef = targetTransform;

        // Step 1: Pick up (move upward slightly)
        attackSequence.Append(transform.DOMoveY(originalPosition.y + 0.5f, 0.2f)
            .SetEase(Ease.OutBack)
            .SetTarget(transform));

        // Step 2: Aggressively move toward the target
        attackSequence.Append(transform.DOMove(attackPosition, attackDuration * 0.8f)
            .SetEase(Ease.InQuad)
            .SetTarget(transform));

        // Step 3: Apply knockback and shake to the target with safety check
        attackSequence.AppendCallback(() =>
        {
            if (isBeingDestroyed || shakeParent == null)
                return;

            // Safety check for target
            if (targetRef == null || !targetRef.gameObject.activeInHierarchy)
            {
                return;
            }

            try
            {
                // Knockback effect on the target with safety check
                targetRef.DOMove(targetRef.position + (targetRef.position - transform.position).normalized * 0.5f, 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(targetRef);

                // Shake effect on the target with safety check
                targetRef.DOShakePosition(0.3f, strength: 0.3f, vibrato: 10, randomness: 90)
                    .SetTarget(targetRef);

                // Trigger the hit callback if it exists
                if (onHitCallback != null)
                {
                    try
                    {
                        onHitCallback.Invoke();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error in hit callback: {e.Message}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during attack animation: {e.Message}");
            }
        });

        // Step 4: Return to the original position with safety check
        attackSequence.Append(transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase)
            .SetTarget(transform));

        // Step 5: Reset rotation and scale with safety checks
        if (shakeParent != null)
        {
            attackSequence.Join(shakeParent.DORotate(Vector3.zero, returnDuration, RotateMode.Fast)
                .SetTarget(shakeParent));
        }

        attackSequence.Join(transform.DOScale(1f, returnDuration)
            .SetEase(Ease.OutBack)
            .SetTarget(transform));

        return attackSequence;
    }


    public Tween AttackedEffect(float intensity = 1.0f, System.Action onCompleteCallback = null)
    {
        if (isBeingDestroyed || shakeParent == null || transform == null)
        {
            return null;
        }

        // Kill any existing tweens on this object
        DOTween.Kill(transform);
        DOTween.Kill(shakeParent);
        if (cardImage != null) DOTween.Kill(cardImage);

        // IMPORTANT: Store original values by value, not by reference
        Vector3 originalPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 originalScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        Quaternion originalRotation = transform.rotation;


        // Create sequence with safety links
        Sequence attackedSequence = DOTween.Sequence();
        attackedSequence.SetId("AttackedEffect");
        attackedSequence.SetLink(gameObject); // Auto-kill if gameObject is destroyed

        // Flash red effect
        if (cardImage != null)
        {
            Color originalColor = cardImage.color;
            attackedSequence.Append(cardImage.DOColor(Color.red, 0.1f)
                .SetTarget(cardImage)
                .OnKill(() => { if (cardImage != null) cardImage.color = originalColor; }));
            attackedSequence.Append(cardImage.DOColor(originalColor, 0.2f)
                .SetTarget(cardImage));
        }

        // Only add these effects if the shakeParent is still valid
        if (shakeParent != null)
        {
            // Shake effect - Make sure to set target so DOTween knows what to check for null
            attackedSequence.Join(shakeParent.DOPunchRotation(
                new Vector3(intensity * 10f, intensity * 5f, intensity * 15f),
                0.3f,
                10,
                0.5f
            ).SetTarget(shakeParent));
        }

        // Vibration effect
        if (transform != null)
        {
            attackedSequence.Join(transform.DOShakePosition(
                0.4f,
                strength: new Vector3(0.2f, 0.2f, 0) * intensity,
                vibrato: 20,
                randomness: 90,
                snapping: false,
                fadeOut: true
            ).SetTarget(transform));

            // Scale punch for impact feeling
            attackedSequence.Join(transform.DOPunchScale(
                new Vector3(-0.3f, -0.3f, 0) * intensity,
                0.3f,
                10,
                0.5f
            ).SetTarget(transform));
        }

        // Ensure we return to original state with null checks
        attackedSequence.OnComplete(() =>
        {

            // Safety check - if object is being destroyed, don't try to modify it
            if (isBeingDestroyed || transform == null)
            {
                return;
            }

            try
            {
                // Reset to original values manually
                transform.position = originalPosition;
                transform.localScale = originalScale;
                transform.rotation = originalRotation;

                // Only invoke callback if we're still valid
                if (onCompleteCallback != null)
                    onCompleteCallback.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting after animation: {e.Message}");
            }
        });

        // Make sure sequence gets killed if object is destroyed
        attackedSequence.OnKill(() =>
        {
            Debug.Log("AttackedEffect sequence was killed");
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

    // Add this to correctly handle object destruction
    private void OnDestroy()
    {
        isBeingDestroyed = true;

        // Kill all tweens associated with this object to prevent errors
        DOTween.Kill(transform);
        if (shakeParent != null) DOTween.Kill(shakeParent);
        if (cardImage != null) DOTween.Kill(cardImage);
        DOTween.Kill("AttackSequence");
        DOTween.Kill("AttackedEffect");
    }

    // Add this to handle disable/enable transitions
    private void OnDisable()
    {
        // Kill animations when disabled to prevent errors
        DOTween.Kill(transform);
        if (shakeParent != null) DOTween.Kill(shakeParent);
        if (cardImage != null) DOTween.Kill(cardImage);
    }
}
