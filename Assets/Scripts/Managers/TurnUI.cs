//using TMPro;
//using UnityEngine;

//public class TurnUI : MonoBehaviour, ITurnObserver
//{
//    [Header("UI References")]
//    [SerializeField] private TextMeshProUGUI turnNumberText;
//    [SerializeField] private TextMeshProUGUI currentActionText;
//    [SerializeField] private GameObject turnStartPanel;
//    [SerializeField] private GameObject turnEndPanel;

//    [Header("Character Turn Display")]
//    [SerializeField] private Transform characterTurnContainer;
//    [SerializeField] private GameObject characterTurnPrefab;

//    private TurnManager turnManager;

//    private void Start()
//    {
//        turnManager = FindObjectOfType<TurnManager>();
//        if (turnManager != null)
//        {
//            turnManager.Subscribe(this);
//        }
//    }

//    private void OnDestroy()
//    {
//        if (turnManager != null)
//        {
//            turnManager.Unsubscribe(this);
//        }
//    }

//    public void OnTurnStart(int turnNumber)
//    {
//        turnNumberText.text = $"Turn {turnNumber}";
//        currentActionText.text = "Processing turns...";

//        // Show turn start animation
//        if (turnStartPanel != null)
//        {
//            turnStartPanel.SetActive(true);
//            StartCoroutine(HidePanel(turnStartPanel, 0.5f));
//        }
//    }

//    public void OnTurnEnd(int turnNumber)
//    {
//        currentActionText.text = "Turn complete";

//        // Show turn end animation
//        if (turnEndPanel != null)
//        {
//            turnEndPanel.SetActive(true);
//            StartCoroutine(HidePanel(turnEndPanel, 0.3f));
//        }
//    }

//    public void OnCharacterTurn(ICharacter character)
//    {
//        currentActionText.text = $"{character.CharacterName} is attacking!";

//        // Highlight attacking character
//        HighlightCharacter(character);
//    }

//    private void HighlightCharacter(ICharacter character)
//    {
//        // Add visual feedback for attacking character
//        Debug.Log($"Highlighting {character.CharacterName} for attack");
//    }

//    private System.Collections.IEnumerator HidePanel(GameObject panel, float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        panel.SetActive(false);
//    }
//}