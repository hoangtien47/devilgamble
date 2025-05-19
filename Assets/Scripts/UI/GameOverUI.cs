using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI btnText;
    [SerializeField] private TextMeshProUGUI damageTakenText;
    [SerializeField] private TextMeshProUGUI bestDamageDealtText;
    [SerializeField] private Button stateButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Text Title Settings")]
    [SerializeField] private string winTitle = "Victory!";
    [SerializeField] private string loseTitle = "Defeat!";

    [Header("Text Button Settings")]
    [SerializeField] private string retryButtonTextWin = "Continue";
    [SerializeField] private string retryButtonTextLose = "Retry";

    private void Awake()
    {
        // Hide panel on start
        if (panel != null) panel.SetActive(false);

        // Set up button listeners
        if (stateButton != null) stateButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    public void Show(bool isWin, int currentDamageDealt, int currentDamageTaken)
    {
        if (panel == null) return;

        // Ensure panel is active but fully transparent
        panel.SetActive(true);
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;

        // Initial state setup
        if (titleText != null)
        {
            titleText.alpha = 0f;
            titleText.text = isWin ? winTitle : loseTitle;
        }

        if (btnText != null)
        {
            btnText.alpha = 0f;
            btnText.text = isWin ? retryButtonTextWin : retryButtonTextLose;
        }

        if (damageTakenText != null)
        {
            damageTakenText.alpha = 0f;
            damageTakenText.text = $"{PlayerPrefs.GetInt("DamageTaken", 0)}";
        }

        if (bestDamageDealtText != null)
        {
            bestDamageDealtText.alpha = 0f;
            bestDamageDealtText.text = $"{PlayerPrefs.GetInt("BestDamageDealt", 0)}";
        }

        // Button setup
        if (stateButton != null)
        {
            stateButton.onClick.RemoveAllListeners();
            stateButton.onClick.AddListener(isWin ? OnContinueClicked : OnRetryClicked);
            stateButton.GetComponent<CanvasGroup>().alpha = 0f;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.GetComponent<CanvasGroup>().alpha = 0f;
        }

        // Create fade-in sequence
        Sequence fadeSequence = DOTween.Sequence();

        // Panel fade in
        fadeSequence.Append(canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad));

        // Title fade in
        if (titleText != null)
        {
            fadeSequence.Append(titleText.DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        // Stats fade in
        if (damageTakenText != null)
        {
            fadeSequence.Append(damageTakenText.DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        if (bestDamageDealtText != null)
        {
            fadeSequence.Join(bestDamageDealtText.DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        // Button text fade in
        if (btnText != null)
        {
            fadeSequence.Append(btnText.DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        // Buttons fade in
        if (stateButton != null)
        {
            fadeSequence.Join(stateButton.GetComponent<CanvasGroup>().DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        if (mainMenuButton != null)
        {
            fadeSequence.Join(mainMenuButton.GetComponent<CanvasGroup>().DOFade(1f, 0.3f).SetEase(Ease.InOutQuad));
        }

        // Play the sequence
        fadeSequence.Play();
    }
    private void OnContinueClicked()
    {
        SceneManager.LoadScene(2);
    }
    private void OnRetryClicked()
    {
        SceneManager.LoadScene(1);
    }

    private void OnMainMenuClicked()
    {
        // Load main menu scene (assuming it's at index 0)
        SceneManager.LoadScene(0);
    }
}