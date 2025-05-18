using System;
using System.Collections.Generic;
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
                // Package and module information for the counter example
                string packageId = "0xd258c05b4e3ea0b6a551acb9054cde9af8ed22e0e11d5a0bce278bbd5bfb8474";
                string moduleName = "counter";

                // You can toggle between these functions to test different operations
                string functionName = "create_and_transfer";  // Create a new counter
                                                              // string functionName = "increment";         // Increment existing counter

                List<object> arguments = new List<object>();

                // If incrementing an existing counter, uncomment and use the object ID
                // if (functionName == "increment") 
                // {
                //     arguments.Add("0xf0c4928833567a19b792f68ea5a838592db371e5128fbee66166e914cdc844ae");
                // }

                statusText.text = $"Executing {moduleName}::{functionName}...";

                // Execute the transaction
                string txDigest = await loginManager.ExecuteTransactionAsync(
                    packageId,
                    moduleName,
                    functionName,
                    arguments,
                    10000000  // Gas budget
                );

                // Process the result
                if (!string.IsNullOrEmpty(txDigest))
                {
                    statusText.text = $"Transaction successful! Digest: {txDigest.Substring(0, Math.Min(8, txDigest.Length))}...";
                    Debug.Log($"Full transaction digest: {txDigest}");
                }
                else
                {
                    statusText.text = "Transaction failed";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Transaction error: {e.Message}");
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