using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class TutorialPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Button closeButton;
    
    [Header("Content References")]
    [SerializeField] private RectTransform contentHolder;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI[] contentTexts;

    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.5f;
    [SerializeField] private float contentDelay = 0.1f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;
    
    [Header("Starting Position")]
    [SerializeField] private Vector2 startPosition = new Vector2(0, -100f);
    [SerializeField] private Vector2 finalPosition = Vector2.zero;

    private void Awake()
    {
        // Initialize panel state
        if (mainPanel == null)
            mainPanel = GetComponent<CanvasGroup>();
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        // Setup initial state
        mainPanel.alpha = 0f;
        mainPanel.interactable = false;
        mainPanel.blocksRaycasts = false;
        panelRect.anchoredPosition = startPosition;

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        // Hide content initially
        if (contentHolder != null)
            contentHolder.localScale = Vector3.zero;
        if (titleText != null)
            titleText.alpha = 0f;
        foreach (var text in contentTexts)
        {
            if (text != null)
                text.alpha = 0f;
        }
    }

    public void ShowPanel()
    {
        // Kill any ongoing tweens
        DOTween.Kill(transform);
        
        // Create sequence
        Sequence showSequence = DOTween.Sequence();

        // Panel slide and fade in
        showSequence.Append(panelRect.DOAnchorPos(finalPosition, openDuration).SetEase(openEase));
        showSequence.Join(mainPanel.DOFade(1f, openDuration * 0.8f).SetEase(fadeEase));

        // Scale in content holder
        if (contentHolder != null)
        {
            showSequence.Append(contentHolder.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack));
        }

        // Fade in title
        if (titleText != null)
        {
            showSequence.Append(titleText.DOFade(1f, scaleDuration).SetEase(fadeEase));
        }

        // Fade in content texts sequentially
        foreach (var text in contentTexts)
        {
            if (text != null)
            {
                showSequence.Append(text.DOFade(1f, scaleDuration).SetEase(fadeEase));
                showSequence.AppendInterval(contentDelay);
            }
        }

        // Enable interaction at the end
        showSequence.OnComplete(() =>
        {
            mainPanel.interactable = true;
            mainPanel.blocksRaycasts = true;
        });

        showSequence.Play();
    }

    public void HidePanel()
    {
        // Kill any ongoing tweens
        DOTween.Kill(transform);

        Sequence hideSequence = DOTween.Sequence();

        // Disable interaction immediately
        mainPanel.interactable = false;
        mainPanel.blocksRaycasts = false;

        // Fade out content texts in reverse order
        for (int i = contentTexts.Length - 1; i >= 0; i--)
        {
            if (contentTexts[i] != null)
            {
                hideSequence.Join(contentTexts[i].DOFade(0f, scaleDuration * 0.5f).SetEase(fadeEase));
            }
        }

        // Fade out title
        if (titleText != null)
        {
            hideSequence.Join(titleText.DOFade(0f, scaleDuration * 0.5f).SetEase(fadeEase));
        }

        // Scale out content holder
        if (contentHolder != null)
        {
            hideSequence.Append(contentHolder.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));
        }

        // Panel slide and fade out
        hideSequence.Append(panelRect.DOAnchorPos(startPosition, openDuration).SetEase(Ease.InBack));
        hideSequence.Join(mainPanel.DOFade(0f, openDuration * 0.8f).SetEase(fadeEase));

        hideSequence.Play();
    }

    private void OnDestroy()
    {
        // Clean up tweens
        DOTween.Kill(transform);
        
        // Remove listeners
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);
    }
}