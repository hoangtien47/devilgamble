using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CardVisual : MonoBehaviour
{
    private bool initalize = false;

    [Header("Card")]
    public Card parentCard;
    private Transform cardTransform;
    private Vector3 rotationDelta;
    private int savedIndex;
    Vector3 movementDelta;
    private Canvas canvas;

    [Header("References")]
    public Transform visualShadow;
    private float shadowOffset = 20;
    private Vector2 shadowDistance;
    private Canvas shadowCanvas;
    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;
    [SerializeField] private Image cardImage;
    [Header("==========UI Character==========")]
    [SerializeField] private TextMeshProUGUI _HPText;
    [SerializeField] private TextMeshProUGUI _ATKText;

    [Header("Follow Parameters")]
    [SerializeField] private float followSpeed = 30;

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;
    [SerializeField] private float rotationSpeed = 20;
    [SerializeField] private float autoTiltAmount = 30;
    [SerializeField] private float manualTiltAmount = 20;
    [SerializeField] private float tiltSpeed = 20;

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;
    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] private float scaleTransition = .15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;

    [Header("Hober Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;
    [SerializeField] private float hoverTransition = .15f;

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;
    [SerializeField] private float swapRotationAngle = 30;
    [SerializeField] private float swapTransition = .15f;
    [SerializeField] private int swapVibrato = 5;

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;

    [Header("Attack Animation")]
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private Ease attackEase = Ease.OutQuint;
    [SerializeField] private Ease returnEase = Ease.OutBack;
    [SerializeField] private float attackDistance = 0.7f; // How close to get to the boss (0-1)

    [Header("Check For Destroy Card")]
    private bool isBeingDestroyed = false;


    private float curveYOffset;
    private float curveRotationOffset;
    private Coroutine pressCoroutine;

    private CardSuit Suit;
    private CardRank Rank;

    [Header("Card Sprites")]
    public CardSpriteDatabase spriteDatabase;


    private void Start()
    {
        shadowDistance = visualShadow.localPosition;
    }

    private void OnDestroy()
    {
        // Set flag when object is being destroyed
        isBeingDestroyed = true;

        // Kill all tweens associated with this object and its children
        DOTween.Kill(transform);
        if (shakeParent != null)
            DOTween.Kill(shakeParent);
        if (tiltParent != null)
            DOTween.Kill(tiltParent);

        // Remove event listeners to prevent callbacks after destruction
        if (parentCard != null)
        {
            parentCard.PointerEnterEvent.RemoveListener(PointerEnter);
            parentCard.PointerExitEvent.RemoveListener(PointerExit);
            parentCard.BeginDragEvent.RemoveListener(BeginDrag);
            parentCard.EndDragEvent.RemoveListener(EndDrag);
            parentCard.PointerDownEvent.RemoveListener(PointerDown);
            parentCard.PointerUpEvent.RemoveListener(PointerUp);
            parentCard.SelectEvent.RemoveListener(Select);
        }
    }

    public void Initialize(Card target, int index = 0)
    {


        Suit = target.Suit;
        Rank = target.Rank;

        int suitIndex = (int)Suit;
        int rankIndex = (int)Rank - 2; // Two starts at 2
        int spriteIndex = suitIndex * 13 + rankIndex;

        if (target.isCharacterCard)
        {
            cardImage.sprite = spriteDatabase.GetIndexCardSprite(target.charIndex);
        }
        else if (!target.isCharacterCard && spriteIndex >= 0 && spriteIndex < spriteDatabase.cardSprites.Length)
        {
            cardImage.sprite = spriteDatabase.GetCardSprite(Suit, Rank);//spriteDatabase.cardSprites[spriteIndex];
        }
        else
        {
            Debug.LogWarning("Invalid sprite index: " + spriteIndex);
        }

        //Declarations
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        //Event Listening
        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);
        parentCard.BeginDragEvent.AddListener(BeginDrag);
        parentCard.EndDragEvent.AddListener(EndDrag);
        parentCard.PointerDownEvent.AddListener(PointerDown);
        parentCard.PointerUpEvent.AddListener(PointerUp);
        parentCard.SelectEvent.AddListener(Select);

        //Initialization
        initalize = true;
    }

    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        HandPositioning();
        SmoothFollow();
        FollowRotation();
        CardTilt();

    }

    private void HandPositioning()
    {
        curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
        curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
    }

    private void SmoothFollow()
    {
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
    }

    private void FollowRotation()
    {
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    private void CardTilt()
    {
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    private void Select(Card card, bool state)
    {
        if (isBeingDestroyed || shakeParent == null) return;

        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20, 1).SetId(2);

        if (scaleAnimations && transform != null)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

    }

    public void Swap(float dir = 1)
    {
        if (isBeingDestroyed || !swapAnimations || shakeParent == null) return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1).SetId(3);
    }

    private void BeginDrag(Card card)
    {
        if (isBeingDestroyed) return;

        if (scaleAnimations && transform != null)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        if (canvas != null)
            canvas.overrideSorting = true;
    }

    private void EndDrag(Card card)
    {
        if (isBeingDestroyed) return;

        if (canvas != null)
            canvas.overrideSorting = false;

        if (transform != null)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerEnter(Card card)
    {
        if (isBeingDestroyed || shakeParent == null) return;

        if (scaleAnimations && transform != null)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    private void PointerExit(Card card)
    {
        if (isBeingDestroyed) return;

        if (!parentCard.wasDragged && transform != null)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerUp(Card card, bool longPress)
    {
        if (isBeingDestroyed) return;

        if (scaleAnimations && transform != null)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);

        if (canvas != null)
            canvas.overrideSorting = false;

        if (visualShadow != null)
        {
            visualShadow.localPosition = shadowDistance;
            if (shadowCanvas != null)
                shadowCanvas.overrideSorting = true;
        }
    }

    private void PointerDown(Card card)
    {
        if (isBeingDestroyed) return;

        if (scaleAnimations && transform != null)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        if (visualShadow != null)
        {
            visualShadow.localPosition += (-Vector3.up * shadowOffset);
            if (shadowCanvas != null)
                shadowCanvas.overrideSorting = false;
        }
    }
    public void OnChangeData(int HP, int ATK)
    {
        _HPText.SetText(HP.ToString());
        _ATKText.SetText(ATK.ToString());
    }
    public Tween Attack(Transform targetTransform, System.Action onHitCallback = null)
    {
        if (isBeingDestroyed || targetTransform == null || transform == null || shakeParent == null)
            return null;

        DOTween.Kill(transform);

        Vector3 originalPosition = transform.position;

        Vector3 attackPosition = Vector3.Lerp(
            originalPosition,
            targetTransform.position,
            attackDistance
        );

        Sequence attackSequence = DOTween.Sequence();

        attackSequence.SetLink(gameObject);
        attackSequence.Append(transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f));

        attackSequence.Join(shakeParent.DORotate(new Vector3(15f, 0f, 0f), 0.2f, RotateMode.LocalAxisAdd));

        attackSequence.Append(transform.DOMove(attackPosition, attackDuration)
            .SetEase(attackEase));

        attackSequence.AppendCallback(() =>
        {
            if (isBeingDestroyed || shakeParent == null) return;

            shakeParent.DOPunchRotation(new Vector3(-25f, 0f, 0f), 0.2f, 10, 0.5f);

            onHitCallback?.Invoke();
        });

        attackSequence.Append(transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase));

        attackSequence.Join(shakeParent.DORotate(Vector3.zero, returnDuration, RotateMode.Fast));

        return attackSequence;
    }


    public Tween SpecialAttack(Transform targetTransform, System.Action onHitCallback = null)
    {
        if (isBeingDestroyed || targetTransform == null || transform == null || shakeParent == null)
            return null;

        DOTween.Kill(transform);

        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        Vector3 attackPosition = Vector3.Lerp(
            originalPosition,
            targetTransform.position,
            attackDistance
        );

        Sequence attackSequence = DOTween.Sequence();

        attackSequence.SetLink(gameObject);

        attackSequence.Append(transform.DOScale(scaleOnSelect * 1.2f, 0.3f).SetEase(Ease.OutBack));

        attackSequence.Join(transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCirc));

        attackSequence.Append(transform.DOPath(
            new Vector3[] {
                originalPosition + Vector3.up * 0.5f,
                attackPosition + Vector3.up * 0.3f,
                attackPosition
            },
            attackDuration * 1.5f,
            PathType.CatmullRom
        ).SetEase(attackEase));

        attackSequence.AppendCallback(() =>
        {
            if (isBeingDestroyed || shakeParent == null) return;

            shakeParent.DOPunchScale(Vector3.one * 0.4f, 0.3f, 10, 0.5f);
            onHitCallback?.Invoke();
        });

        attackSequence.Append(transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase));

        attackSequence.Join(transform.DORotateQuaternion(originalRotation, returnDuration));
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
}
