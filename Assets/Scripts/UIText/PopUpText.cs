using DG.Tweening;
using TMPro;
using UnityEngine;

public class PopUpText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;

    public void Show(string message, Color color, float moveDistance = 80f, float duration = 1f)
    {
        textMesh.text = message;
        textMesh.color = color;

        // Reset scale for repeated use
        transform.localScale = Vector3.one;

        // Pop effect: scale up quickly, then back to normal
        transform.DOScale(1.5f, 0.15f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => transform.DOScale(1f, 0.15f).SetEase(Ease.InBack));

        // Pick a random direction (normalized 2D vector)
        float angle = Random.Range(0, 2 * Mathf.PI);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 targetPos = ((RectTransform)transform).anchoredPosition + direction * moveDistance;

        // Animate movement and fade
        ((RectTransform)transform).DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic);
        textMesh.DOFade(0, duration);

        // Destroy after animation
        Destroy(gameObject, duration);
    }
}
