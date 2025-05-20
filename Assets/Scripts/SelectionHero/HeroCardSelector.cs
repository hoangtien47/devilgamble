using DG.Tweening;
using Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroCardSelector : MonoBehaviour
{
    private bool initialized = false;

    [Header("Card")]
    public HeroSelectData parentData;
    private Transform cardTransform;
    private Vector3 rotationDelta;
    private int savedIndex;
    private Vector3 movementDelta;
    private Canvas canvas;

    [Header("UI Elements")]
    public Transform visualShadow;
    private float shadowOffset = 20;
    private Vector2 shadowDistance;
    private Canvas shadowCanvas;
    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;
    [SerializeField] public Image cardImage;
    [SerializeField] private TextMeshProUGUI _HPText;
    [SerializeField] private TextMeshProUGUI _ATKText;
    [SerializeField] private TextMeshProUGUI _NameText;

    [Header("Animation Settings")]
    [SerializeField] private float followSpeed = 30f;
    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] private float scaleTransition = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationAmount = 20f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float autoTiltAmount = 30f;
    [SerializeField] private float manualTiltAmount = 20f;
    [SerializeField] private float tiltSpeed = 20f;

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;

    [Header("Swap Animation")]
    [SerializeField] private float swapRotationAngle = 30f;
    [SerializeField] private float swapTransition = 0.15f;
    [SerializeField] private int swapVibrato = 5;

    private bool isSelected = false;
    private bool isBeingDestroyed = false;
    private Tween currentTween;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private float curveYOffset;
    private float curveRotationOffset;

    [Header("Lock UI")]
    [SerializeField] private GameObject lockImage; // Reference to the lock UI image
    private bool isUnlocked = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalScale = transform.localScale;
    }

    public virtual void Initialize(HeroSelectData target)
    {
        if (initialized) return;

        parentData = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();
        // Event Listening
        if (parentData != null)
        {
            parentData.PointerEnterEvent.AddListener(OnPointerEnter);
            parentData.PointerExitEvent.AddListener(OnPointerExit);
            parentData.PointerDownEvent.AddListener(OnPointerDown);
            parentData.PointerUpEvent.AddListener(OnPointerUp);
            parentData.BeginDragEvent.AddListener(OnBeginDrag);
            parentData.EndDragEvent.AddListener(OnEndDrag);
            parentData.SelectEvent.AddListener(OnSelect);
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || parentData == null) return;

        UpdatePosition();
        UpdateRotation();
        UpdateTilt();
    }

    private void UpdatePosition()
    {
        curveYOffset = curve != null ?
            (curve.positioning.Evaluate(parentData.NormalizedPosition()) * curve.positioningInfluence) * parentData.SiblingAmount() : 0;

        if (parentData.SiblingAmount() < 5) curveYOffset = 0;

        Vector3 targetPos = cardTransform.position + (Vector3.up * (parentData.isDragging ? 0 : curveYOffset));
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    private void UpdateRotation()
    {
        Vector3 movement = transform.position - cardTransform.position;
        movementDelta = Vector3.Lerp(movementDelta, movement, 25f * Time.deltaTime);
        Vector3 rotationAmount = (parentData.isDragging ? movementDelta : movement) * this.rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, rotationAmount, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            Mathf.Clamp(rotationDelta.x, -60f, 60f)
        );
    }

    private void UpdateTilt()
    {
        if (tiltParent == null) return;

        savedIndex = parentData.isDragging ? savedIndex : parentData.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentData.isHovering ? 0.2f : 1f);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentData.isHovering ? 0.2f : 1f);

        Vector3 mouseOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float tiltX = parentData.isHovering ? (-mouseOffset.y * manualTiltAmount) : 0f;
        float tiltY = parentData.isHovering ? (mouseOffset.x * manualTiltAmount) : 0f;

        curveRotationOffset = curve != null ?
            curve.rotation.Evaluate(parentData.NormalizedPosition()) : 0;
        float tiltZ = parentData.isDragging ?
            tiltParent.eulerAngles.z :
            (curveRotationOffset * (curve.rotationInfluence * parentData.SiblingAmount()));

        tiltParent.eulerAngles = new Vector3(
            Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime),
            Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime),
            Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed * 0.5f * Time.deltaTime)
        );
    }

    private void OnPointerEnter(HeroSelectData card)
    {
        if (isBeingDestroyed || isSelected || !initialized || parentData == null) return;

        KillCurrentTween();

        // Simple scale up animation
        currentTween = transform.DOScale(scaleOnHover * originalScale, scaleTransition)
            .SetEase(scaleEase);
    }

    private void OnPointerExit(HeroSelectData card)
    {
        if (isBeingDestroyed || isSelected || !initialized || parentData == null) return;

        KillCurrentTween();

        // Simple scale down animation
        currentTween = transform.DOScale(originalScale, scaleTransition)
            .SetEase(scaleEase);
    }
    public virtual void HealHero()
    {
        // Create heal animation sequence
        Sequence healSequence = DOTween.Sequence();
        Debug.Log("Healing hero: " + parentData.heroData.Name);
        // Store original values
        Vector3 originalScale = parentData.transform.localScale;
        Color originalColor = cardImage.color;
        Color healColor = Color.green; // Healing color

        // Create glow/heal effect
        healSequence.Append(transform
            .DOScale(originalScale * 1.2f, 0.3f)
            .SetEase(Ease.OutBack));

        // Color change animation
        healSequence.Join(cardImage
            .DOColor(healColor, 0.3f)
            .SetEase(Ease.InOutSine));

        // Wait a moment
        healSequence.AppendInterval(0.2f);

        // Return to original color
        healSequence.Append(cardImage
            .DOColor(originalColor, 0.3f)
            .SetEase(Ease.InOutSine));

        // Return to original scale
        healSequence.Join(transform
            .DOScale(originalScale, 0.3f)
            .SetEase(Ease.OutBack));

        // Shake effect for emphasis
        healSequence.Join(transform
            .DOShakePosition(0.3f, strength: 0.2f, vibrato: 10)
            .SetEase(Ease.OutQuad));

        // Update card holder and unlock map after animation
        healSequence.OnComplete(() =>
        {
            var tracker = FindObjectOfType<MapPlayerTracker>();
            if (tracker != null)
            {
                tracker.Locked = false;
            }
        });

        healSequence.Play();
    }
    private void OnPointerDown(HeroSelectData card)
    {
        if (isBeingDestroyed) return;

        KillCurrentTween();
        transform.DOScale(scaleOnSelect * originalScale, scaleTransition)
            .SetEase(scaleEase);
    }

    private void OnPointerUp(HeroSelectData card, bool longPress)
    {
        if (isBeingDestroyed) return;

        KillCurrentTween();
        float targetScale = longPress ? scaleOnHover : (isSelected ? scaleOnSelect : 1f);
        transform.DOScale(originalScale * targetScale, scaleTransition)
            .SetEase(scaleEase);
    }
        
    private void OnBeginDrag(HeroSelectData card)
    {
        if (isBeingDestroyed) return;
        if (canvas != null) canvas.overrideSorting = true;
    }

    private void OnEndDrag(HeroSelectData card)
    {
        if (isBeingDestroyed) return;
        if (canvas != null) canvas.overrideSorting = false;
    }

    private void OnSelect(HeroSelectData card, bool selected)
    {
        isSelected = selected;

        KillCurrentTween();
        transform.DOScale(selected ? scaleOnSelect * originalScale : originalScale, scaleTransition)
            .SetEase(scaleEase);
    }
    
    public virtual void LoadHeroData(HeroCardScriptable heroData)
    {
        if (heroData == null) return;
        Debug.Log("Name draw map:" + heroData.name);
        cardImage.sprite = heroData.Sprite;
        _HPText.text = heroData.currentHealth.ToString();
        _ATKText.text = heroData.attack.ToString();
        _NameText.text = heroData.Name;

        // Handle lock state
        lockImage.SetActive(!heroData.isUnlocked);
    }

    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();
    }

    private void OnDestroy()
    {
        isBeingDestroyed = true;
        KillCurrentTween();
        DOTween.Kill(transform);

        if (initialized && parentData != null)
        {
            parentData.PointerEnterEvent.RemoveListener(OnPointerEnter);
            parentData.PointerExitEvent.RemoveListener(OnPointerExit);
            parentData.PointerDownEvent.RemoveListener(OnPointerDown);
            parentData.PointerUpEvent.RemoveListener(OnPointerUp);
            parentData.BeginDragEvent.RemoveListener(OnBeginDrag);
            parentData.EndDragEvent.RemoveListener(OnEndDrag);
            parentData.SelectEvent.RemoveListener(OnSelect);
        }
    }
    public void UpdateIndex(int totalCount)
    {
        if (!initialized || parentData == null) return;

        // Get the card's position in the sequence
        int index = parentData.ParentIndex();

        // Update canvas sorting order to maintain proper layering
        if (canvas != null)
        {
            canvas.sortingOrder = totalCount - index;
        }

        // Update shadow if present
        if (visualShadow != null && shadowCanvas != null)
        {
            shadowCanvas.sortingOrder = totalCount - index - 1;
        }

        // Update curve-based positioning
        if (curve != null)
        {
            curveYOffset = (curve.positioning.Evaluate(parentData.NormalizedPosition()) * curve.positioningInfluence) * parentData.SiblingAmount();
            curveRotationOffset = curve.rotation.Evaluate(parentData.NormalizedPosition());
        }
    }

    public void Swap(float direction)
    {
        if (isBeingDestroyed || !initialized || shakeParent == null) return;

        // Kill any existing swap animation
        DOTween.Kill(3);

        // Create swap animation sequence
        var sequence = DOTween.Sequence();

        // Punch rotation effect
        sequence.Append(
            shakeParent.DOPunchRotation(
                Vector3.forward * swapRotationAngle * direction,
                swapTransition,
                swapVibrato,
                1f
            ).SetId(3)
        );

        // Optional scale punch for emphasis
        sequence.Join(
            transform.DOPunchScale(
                Vector3.one * 0.1f,
                swapTransition,
                swapVibrato,
                1f
            )
        );

        // Update card visual state
        if (parentData != null)
        {
            currentTween = sequence;
        }
    }
}