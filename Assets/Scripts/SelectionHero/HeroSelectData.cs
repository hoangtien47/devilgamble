using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class HeroSelectData : MonoBehaviour, IPointerEnterHandler, IBeginDragHandler, IEndDragHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private VisualCardsHandler visualHandler;
    private Vector3 offset;

    [HideInInspector] public HeroCardScriptable heroData;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("Visual")]
    [SerializeField] private GameObject heroVisualPrefab;
    [HideInInspector] public HeroCardSelector heroVisual;

    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;

    [Header("Events")]
    [HideInInspector] public UnityEvent<HeroSelectData> PointerEnterEvent;
    [HideInInspector] public UnityEvent<HeroSelectData> PointerExitEvent;
    [HideInInspector] public UnityEvent<HeroSelectData, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<HeroSelectData> PointerDownEvent;
    [HideInInspector] public UnityEvent<HeroSelectData> BeginDragEvent;
    [HideInInspector] public UnityEvent<HeroSelectData> EndDragEvent;
    [HideInInspector] public UnityEvent<HeroSelectData, bool> SelectEvent;

    public int idx;
    private void Awake()
    {
        
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();
        if (!instantiateVisual)
            return;

        visualHandler = FindObjectOfType<VisualCardsHandler>();
        heroVisual = Instantiate(heroVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<HeroCardSelector>();

        // Initialize the visual with this data and setup events
        if (heroVisual != null)
        {
            heroVisual.Initialize(this);
            if (heroData != null)
            {
                heroVisual.LoadHeroData(heroData);
            }
        }
    }
    private void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }
    void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        PointerEnterEvent?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PointerExitEvent?.Invoke(this);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        selected = !selected;
        SelectEvent.Invoke(this, selected);

        if (selected)
            transform.localPosition += (heroVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }
    public virtual void OnLoadCharacterData(HeroCardScriptable character)
    {
        if (heroVisual != null)
            heroVisual.LoadHeroData(character);
    }
    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            if (selected)
            {
                SelectEvent.Invoke(this, false);  // Make sure to trigger the event when deselecting
                transform.localPosition += (heroVisual.transform.up * 50);
            }
            else
                transform.localPosition = Vector3.zero;
        }
    }


    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    private void OnDestroy()
    {
        // Kill any tweens associated with this card
        DOTween.Kill(transform);

        // Clear all event listeners
        PointerEnterEvent.RemoveAllListeners();
        PointerExitEvent.RemoveAllListeners();
        PointerUpEvent.RemoveAllListeners();
        PointerDownEvent.RemoveAllListeners();
        SelectEvent.RemoveAllListeners();

        // Destroy the card visual if it exists
        if (heroVisual != null)
        {
            // The cardVisual will handle its own tween cleanup in its OnDestroy
            Destroy(heroVisual.gameObject);
        }
    }
}