using Sui.Cryptography.Ed25519;
using Sui.ZKLogin.SDK;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZkLogin
{
    public class GoogleAuthManager : MonoBehaviour
    {
        [SerializeField] private GoogleAuthConfig config;
        [SerializeField] private string rpcUrl = "https://fullnode.devnet.sui.io"; // Add RPC URL

        private TaskCompletionSource<string> authCompletionSource;
        private string ephemeralPrivateKeyB64; // Store ephemeral key
        private PublicKey ephemeralPublicKey; // Store public key
        private ulong maxEpoch; // Store maxEpoch
        private string randomness; // Store randomness

        public event Action<string> OnAuthSuccess;
        public event Action<string> OnAuthError;

        // Properties to access the generated values later
        public string EphemeralPrivateKey => ephemeralPrivateKeyB64;
        public ulong MaxEpoch => maxEpoch;
        public string Randomness => randomness;

#if UNITY_EDITOR
        private EditorOAuthServer editorServer;
#endif

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("GoogleAuthConfig is not assigned to GoogleAuthManager");
            }

            // Ensure there's only one instance of this manager
            DontDestroyOnLoad(gameObject);
        }

        public async Task<string> AuthenticateWithGoogleAsync()
        {
            if (config == null)
            {
                throw new InvalidOperationException("GoogleAuthConfig is not assigned!");
            }

            authCompletionSource = new TaskCompletionSource<string>();

            try
            {
                // Generate ephemeral key and nonce before starting auth flow
                await GenerateEphemeralKeyAndNonce();

                // Build the OAuth URL with appropriate redirect URI
                string redirectUri = config.GetRedirectUri();
                string authUrl = BuildAuthorizationUrl(redirectUri);
                Debug.Log($"Opening OAuth URL: {authUrl}");

#if UNITY_EDITOR
                // In editor, start local server and open browser
                await StartEditorAuthenticationAsync(authUrl);
#elif UNITY_WEBGL
                // In WebGL, open a popup window via JS interop
                OpenAuthWindowWebGL(authUrl);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication setup error: {ex.Message}");
                authCompletionSource.TrySetException(ex);
            }

            return await authCompletionSource.Task;
        }

        private async Task GenerateEphemeralKeyAndNonce()
        {
            try
            {
                // Generate random ephemeral private key
                byte[] ephemeralKeyBytes = NonceGenerator.RandomBytes();
                ephemeralPrivateKeyB64 = Convert.ToBase64String(ephemeralKeyBytes);

                // Create key pair
                var ephemeralPrivateKey = new PrivateKey(ephemeralPrivateKeyB64);
                var ephemeralPublicKeyBase = ephemeralPrivateKey.PublicKey();
                ephemeralPublicKey = new PublicKey(ephemeralPublicKeyBase.KeyBase64);

                Debug.Log($"Google Auth Private key: {ephemeralPrivateKey}");
                Debug.Log($"Google Auth Public key: {ephemeralPublicKey}");

                // Get current epoch from Sui RPC
                maxEpoch = await FetchCurrentEpochFromRPC() + 10; // Buffer of 10 epochs

                // Generate randomness
                randomness = NonceGenerator.GenerateRandomness();

                Debug.Log($"Generated ephemeral key pair. Public key: {ephemeralPublicKey.KeyBase64}");
                Debug.Log($"Current max epoch: {maxEpoch}");

                // Save ephemeral key for later use (consider a more secure storage in production)
                PlayerPrefs.SetString("ephemeralPrivateKey", ephemeralPrivateKeyB64);
                PlayerPrefs.SetString("randomness", randomness);
                PlayerPrefs.SetString("maxEpoch", maxEpoch.ToString());
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating ephemeral key and nonce: {e}");
                throw;
            }
        }

        private async Task<ulong> FetchCurrentEpochFromRPC()
        {
            // Create a temporary client to fetch current epoch
            var client = new SuiRpcClient(rpcUrl);
            return await client.GetCurrentEpochAsync();
        }

        private string BuildAuthorizationUrl(string redirectUri)
        {
            // Generate nonce with the full method using ephemeral key, epoch and randomness
            string nonce = NonceGenerator.GenerateNonce(
                ephemeralPublicKey,
                (int)maxEpoch,
                randomness
            );
            Debug.Log($"Google Generated nonce for proof: {nonce}");


            var parameters = new Dictionary<string, string>
            {
                { "client_id", config.clientId },
                { "redirect_uri", redirectUri },
                { "response_type", "id_token" },
                { "scope", config.scopes },
                { "nonce", nonce } // Using advanced ZKLogin nonce
            };

            var queryString = new System.Text.StringBuilder();
            foreach (var param in parameters)
            {
                if (queryString.Length > 0) queryString.Append('&');
                queryString.Append(Uri.EscapeDataString(param.Key));
                queryString.Append('=');
                queryString.Append(Uri.EscapeDataString(param.Value));
            }

            return $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";
        }

#if UNITY_EDITOR
        private async Task StartEditorAuthenticationAsync(string authUrl)
        {
            try
            {
                // Start local server to receive OAuth callback
                editorServer = new EditorOAuthServer(config.editorPort);

                // Start local server first
                var serverTask = editorServer.StartListeningAsync();

                // Open the default browser with the auth URL
                Application.OpenURL(authUrl);

                // Wait for the token from the server
                string token = await serverTask;
                CompleteAuthWithSuccess(token);
            }
            catch (Exception ex)
            {
                CompleteAuthWithError($"Editor authentication error: {ex.Message}");
            }
        }
#endif

#if UNITY_WEBGL
        private void OpenAuthWindowWebGL(string authUrl)
        {
            // Call our JS plugin function
            OpenAuthWindow(authUrl);
        }

        // Called by JSLib plugin when authentication succeeds
        public void OnWebGLTokenReceived(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log("Received token from WebGL callback");
                CompleteAuthWithSuccess(token);
            }
        }

        // Called by JSLib plugin when authentication fails
        public void OnWebGLAuthError(string error)
        {
            Debug.LogError($"WebGL Auth Error: {error}");
            CompleteAuthWithError(error);
        }

        // JS interface functions
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern bool OpenAuthWindow(string url);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CloseAuthWindow();
#endif

        private void CompleteAuthWithSuccess(string token)
        {
            Debug.Log("Authentication successful");
            OnAuthSuccess?.Invoke(token);
            authCompletionSource?.TrySetResult(token);
        }

        private void CompleteAuthWithError(string error)
        {
            Debug.LogError($"Authentication error: {error}");
            OnAuthError?.Invoke(error);
            authCompletionSource?.TrySetException(new Exception(error));
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            editorServer?.Dispose();
#elif UNITY_WEBGL
            CloseAuthWindow();
#endif
        }
    }
}