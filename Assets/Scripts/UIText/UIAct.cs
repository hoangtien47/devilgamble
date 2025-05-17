using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIAct : MonoBehaviour
{
    [Header("Popup Prefab")]
    [SerializeField] private GameObject popupPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ShowPopup(int amount, bool isHeal)
    {
        if (popupPrefab == null) return;

        // Find the parent canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Instantiate popup as child of canvas
        GameObject popup = Instantiate(popupPrefab, canvas.transform);
        popup.transform.SetAsLastSibling();
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        // Convert this object's world position to canvas local position
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out anchoredPos
        );
        popupRect.anchoredPosition = anchoredPos;

        // Set up popup text and animate
        PopUpText popUpText = popup.GetComponent<PopUpText>();
        if (popUpText != null)
        {
            string sign = isHeal ? "+" : "-";
            Color color = isHeal ? Color.green : Color.red;
            popUpText.Show($"{sign}{Mathf.Abs(amount)}", color);
        }
    }
}
