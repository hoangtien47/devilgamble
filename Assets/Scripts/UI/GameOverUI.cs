using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI btnText;

    [SerializeField] private Button stateButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Text Title Settings")]
    [SerializeField] private string winTitle = "Victory!";
    [SerializeField] private string loseTitle = "Defeat!";

    [Header("Text Button Settings")]
    [SerializeField] private string retryButtonTextWin = "Continue";
    [SerializeField] private string retryButtonTextLose = "Retry";
    [SerializeField] private string newGameButtonText = "New Game";


    [SerializeField] private TurnManager turnManager;

    private void OnEnable()
    {
        TurnManager.OnGameOver += Show;
    }

    private void OnDisable()
    {
        TurnManager.OnGameOver -= Show;
    }

    private void Awake()
    {
        // Hide panel on start
        if (panel != null) panel.SetActive(false);

        // Set up button listeners
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(
            GameManager.Instance.OpenLobby);
    }

    public void Show(bool isWin, bool isComplete)
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
        Debug.Log("Game Completed: " + isComplete);
        if (btnText != null)
        {
            btnText.alpha = 0f;
            string textWin = isComplete ? newGameButtonText : retryButtonTextWin;
            btnText.text = isWin ? textWin: retryButtonTextLose;
        }



        // Button setup
        if (stateButton != null)
        {
            stateButton.onClick.RemoveAllListeners();
            if (isWin)
            {
                stateButton.onClick.AddListener(isComplete ?
                GameManager.Instance.OpenLevelScence : GameManager.Instance.OpenMapScence);
            }
            else
            {
                stateButton.onClick.AddListener(GameManager.Instance.OpenBattleScence);
            }
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

}