using UnityEngine;

public class WalletUI : MonoBehaviour
{
    private static WalletUI instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
