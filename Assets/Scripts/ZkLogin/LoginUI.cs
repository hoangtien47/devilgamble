using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZkLogin
{
    public class ZkLoginUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button signInButton;
        [SerializeField] private Button generateProofButton;
        [SerializeField] private Button signTransactionButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text addressText;

        [Header("References")]
        [SerializeField] private LoginManager loginManager;

        private bool isSignedIn = false;
        private bool isProofGenerated = false;

        private void Start()
        {
            // Set up button listeners
            signInButton.onClick.AddListener(OnSignInClicked);
            generateProofButton.onClick.AddListener(OnGenerateProofClicked);
            signTransactionButton.onClick.AddListener(OnSignTransactionClicked);

            // Set up login manager events
            loginManager.OnSignIn += OnSignedIn;
            loginManager.OnProofGenerated += OnProofGenerated;
            loginManager.OnError += OnError;

            // Update UI state
            UpdateUIState();
        }

        private async void OnSignInClicked()
        {
            statusText.text = "Signing in...";
            signInButton.interactable = false;

            try
            {
                await loginManager.InitSignIn();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error starting sign-in: {e.Message}");
                statusText.text = $"Error: {e.Message}";
                signInButton.interactable = true;
            }
        }

        private async void OnGenerateProofClicked()
        {
            statusText.text = "Generating ZK proof...";
            generateProofButton.interactable = false;

            try
            {
                await loginManager.GenerateZkProofAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating proof: {e.Message}");
                statusText.text = $"Proof error: {e.Message}";
                generateProofButton.interactable = true;
            }
        }

        private async void OnSignTransactionClicked()
        {
            statusText.text = "Preparing transaction...";
            signTransactionButton.interactable = false;

            try
            {
                // This is a sample transaction - in a real app, you would build a real transaction
                string sampleTransaction = CreateSampleTransaction();

                statusText.text = "Signing and executing transaction...";

                // Since SignAndExecuteTransaction isn't implemented yet, just show placeholder message
                // In a real app, you would uncomment this line when implemented:
                // string txResult = await loginManager.SignAndExecuteTransaction(sampleTransaction);

                // For now, just show a placeholder message
                statusText.text = "Transaction feature coming soon!";

                /* Uncomment when transaction functionality is implemented:
                if (!string.IsNullOrEmpty(txResult))
                {
                    statusText.text = $"Transaction successful! Digest: {txResult.Substring(0, 8)}...";
                }
                else
                {
                    statusText.text = "Transaction failed";
                }
                */
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Transaction error: {e.Message}");
                statusText.text = $"Transaction error: {e.Message}";
            }
            finally
            {
                signTransactionButton.interactable = true;
            }
        }

        private void OnSignedIn(string username, string token, string address)
        {
            isSignedIn = true;
            addressText.text = $"Address: {address}";
            statusText.text = $"Signed in as {username}";
            UpdateUIState();
        }

        private void OnProofGenerated(string address)
        {
            isProofGenerated = true;
            statusText.text = "ZK proof generated successfully!";
            UpdateUIState();
        }

        private void OnError(string errorMessage)
        {
            statusText.text = $"Error: {errorMessage}";
        }

        private void UpdateUIState()
        {
            signInButton.gameObject.SetActive(!isSignedIn);
            generateProofButton.gameObject.SetActive(isSignedIn && !isProofGenerated);
            signTransactionButton.gameObject.SetActive(isSignedIn && isProofGenerated);
            addressText.gameObject.SetActive(isSignedIn);
        }

        private string CreateSampleTransaction()
        {
            // This is where you would create a real Sui transaction
            // For testing, you can return a placeholder
            return "SAMPLE_TRANSACTION_BYTES";
        }

        private void OnDestroy()
        {
            // Clean up event listeners
            if (loginManager != null)
            {
                loginManager.OnSignIn -= OnSignedIn;
                loginManager.OnProofGenerated -= OnProofGenerated;
                loginManager.OnError -= OnError;
            }
        }
    }
}