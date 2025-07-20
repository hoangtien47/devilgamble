using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    Button myButton;
    private ContractManager _contractManager;

    private void Start()
    {
        myButton = GetComponent<Button>();
        _contractManager = FindFirstObjectByType<ContractManager>();
        myButton.onClick.AddListener(async () =>
        {
            List<CharacterCardData> cards = await _contractManager.GetAllCharacterNFT();

            GameManager.Instance.OpenLevelScence();
        });
    }
}
