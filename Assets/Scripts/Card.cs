
using DG.Tweening;
using Map;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum CardSuit { Hearts, Diamonds, Clubs, Spades }
public enum CardRank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private VisualCardsHandler visualHandler;
    private Vector3 offset;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab;
    [HideInInspector] public CardVisual cardVisual;

    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;
    [HideInInspector] public BaseCharacter BaseCharacter;

    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;
    public CardSuit Suit { get; set; }
    public CardRank Rank { get; set; }

    public bool isCharacterCard { get; set; }
    public int charIndex { get; set; }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();
        if (!instantiateVisual)
            return;

        visualHandler = FindObjectOfType<VisualCardsHandler>();
        cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        if (isCharacterCard)
        {
            cardVisual.OnLoadCharacter(BaseCharacter);
        }
        cardVisual.Initialize(this);
    }

    void Update()
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
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

        if (wasDragged)
            return;

        selected = !selected;
        SelectEvent.Invoke(this, selected);

        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }
    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            if (selected)
            {
                SelectEvent.Invoke(this, false);  // Make sure to trigger the event when deselecting
                transform.localPosition += (cardVisual.transform.up * 50);
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

    public virtual void OnCharacterDataChange()
    {
        if (cardVisual != null)
            cardVisual.OnChangeData(GetComponent<BaseCharacter>().HP, GetComponent<BaseCharacter>().ATK);
    }
    public virtual void OnLoadCharacterData(BaseCharacter character)
    {
        if (cardVisual != null)
            cardVisual.OnLoadCharacter(character);
    }
    public virtual void LoadCharacterData(EnemyCardData enemyCardData)
    {
        if(GetComponent<EnemyCharacter>() != null)
        {
            Debug.Log("LoadCharacterData");
            GetComponent<EnemyCharacter>().SetData(enemyCardData);
            OnLoadCharacterData(GetComponent<EnemyCharacter>());
        }
    }
    public virtual void LoadCharacterData(HeroCardData hero)
    {
        if (GetComponent<HeroesCharacter>() != null)
        {
            GetComponent<HeroesCharacter>().SetData(hero);
            OnLoadCharacterData(GetComponent<HeroesCharacter>());
        }
    }
    public virtual void OnCharacterDeath()
    {
        if (cardVisual != null)
        {
            cardVisual.PlayExplosionEffect();
            Destroy(cardVisual.gameObject);
            Destroy(this.gameObject, 1f);
        }
    }
    public virtual void OnAttack(ICharacter target)
    {
        if (cardVisual != null)
            this.GetComponent<BaseCharacter>().Attack(target);
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
        BeginDragEvent.RemoveAllListeners();
        EndDragEvent.RemoveAllListeners();
        SelectEvent.RemoveAllListeners();

        // Destroy the card visual if it exists
        if (cardVisual != null)
        {
            // The cardVisual will handle its own tween cleanup in its OnDestroy
            Destroy(cardVisual.gameObject);
        }
    }
}
