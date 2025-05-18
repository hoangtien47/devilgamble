using DG.Tweening;
using TMPro;
using UnityEngine;

public class TooltipSelectedHeroes : MonoBehaviour
{
    private static TooltipSelectedHeroes instance;
    public static TooltipSelectedHeroes Instance { get { return instance; } }

    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 0.2f;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

        // Hide tooltip initially
        HideTooltip();
    }

    public void ShowTooltip(HeroCardScriptable heroData)
    {
        if (heroData == null) return;

        if (!heroData.isUnlocked)
        {
            // Display ??? for locked heroes
            nameText.text = "???";
            hpText.text = "???";
            atkText.text = "???";
        }
        else
        {
            nameText.text = heroData.Name;
            hpText.text = $"{heroData.health}";
            atkText.text = $"{heroData.attack}";
        }
        // Show and fade in
        tooltipPanel.SetActive(true);
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(1f, fadeSpeed);
    }

    public void HideTooltip()
    {
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(0f, fadeSpeed)
            .OnComplete(() => tooltipPanel.SetActive(false));
    }
}
