using Sui.ZKLogin;
using Sui.ZKLogin.SDK;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace ZkLogin
{
    public class LoginManager : MonoBehaviour
    {
        [SerializeField] private string rpcUrl = "https://fullnode.devnet.sui.io";
        [SerializeField] private GoogleAuthManager googleAuthManager;
        [SerializeField] private GoogleAuthConfig authConfig;

        public event Action<string, string, string> OnSignIn; // username, access token, address
        public event Action<string> OnProofGenerated;
        public event Action<string> OnError;

        private string username;
        private string suiAddress;
        private string userSalt;
        private string accessToken;
        private string ephemeralPrivateKey;
        private ulong maxEpoch;
        private string randomness;

        // State
        private bool isProofGenerated = false;
        private ZkLoginSignature zkSignature;

        private void Start()
        {
            // Create GoogleAuthManager if not already assigned
            if (googleAuthManager == null)
            {
                var existingManager = FindObjectOfType<GoogleAuthManager>();
                if (existingManager != null)
                {
                    googleAuthManager = existingManager;
                }
                else
                {
                    GameObject authObj = new GameObject("GoogleAuthManager");
                    googleAuthManager = authObj.AddComponent<GoogleAuthManager>();
                    if (authConfig != null)
                    {
                        // Set config through reflection since it's a SerializeField
                        var configField = typeof(GoogleAuthManager).GetField("config",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (configField != null) configField.SetValue(googleAuthManager, authConfig);
                    }
                }
            }

            // Subscribe to Google auth events
            googleAuthManager.OnAuthSuccess += OnGoogleAuthSuccess;
            googleAuthManager.OnAuthError += OnAuthErrorHandler;
        }

        public async Task InitSignIn()
        {
            try
            {
                // Start Google authentication flow
                accessToken = await googleAuthManager.AuthenticateWithGoogleAsync();
                Debug.Log("Successfully received Google ID token");

                // Parse JWT to get user info
                var payload = CustomJWTDecoder.DecodePayload(accessToken);
                username = payload.name ?? payload.email ?? "User";

                // Generate Sui address
                await GenerateSuiAddressFromJwt(accessToken);

                OnSignIn?.Invoke(username, accessToken, suiAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing in with Google: {e}");
                OnError?.Invoke($"Sign-in error: {e.Message}");
            }
        }

        private void OnGoogleAuthSuccess(string token)
        {
            // Store token for later use
            accessToken = token;
            Debug.Log("Google authentication successful");

            // Parse the token to get user information if needed
            try
            {
                var payload = CustomJWTDecoder.DecodePayload(token);
                username = payload.name ?? payload.email ?? "User";
                Debug.Log($"Logged in as: {username}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not parse JWT payload: {e.Message}");
            }
        }

        private void OnAuthErrorHandler(string errorMsg)
        {
            OnError?.Invoke(errorMsg);
        }

        private async Task GenerateSuiAddressFromJwt(string jwtToken)
        {
            try
            {
                var payload = CustomJWTDecoder.DecodePayload(jwtToken);
                Debug.Log($"Using JWT with issuer: {payload.iss}, subject: {payload.sub}");

                // Load or create salt for this user
                userSalt = LoadOrCreateSalt(payload.sub);

                // Generate Sui address using the JWT
                suiAddress = Sui.ZKLogin.SDK.Address.JwtToAddress(jwtToken, userSalt);
                Debug.Log($"Generated Sui address: {suiAddress}");

                // Use the previously generated key or create a new one
                await LoadOrGenerateEphemeralKeyAndNonce();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating Sui address: {e}");
                OnError?.Invoke($"Address generation error: {e.Message}");
            }
        }

        private async Task LoadOrGenerateEphemeralKeyAndNonce()
        {
            try
            {
                // Try to get ephemeral key directly from GoogleAuthManager if available
                if (googleAuthManager != null && !string.IsNullOrEmpty(googleAuthManager.EphemeralPrivateKey))
                {
                    ephemeralPrivateKey = googleAuthManager.EphemeralPrivateKey;
                    maxEpoch = googleAuthManager.MaxEpoch;
                    randomness = googleAuthManager.Randomness;

                    Debug.Log("Using ephemeral key and nonce from GoogleAuthManager");
                }
                else
                {
                    // Try to load from PlayerPrefs
                    ephemeralPrivateKey = PlayerPrefs.GetString("ephemeralPrivateKey", string.Empty);
                    string maxEpochStr = PlayerPrefs.GetString("maxEpoch", string.Empty);
                    randomness = PlayerPrefs.GetString("randomness", string.Empty);

                    // Check if we found valid values
                    if (!string.IsNullOrEmpty(ephemeralPrivateKey) && !string.IsNullOrEmpty(maxEpochStr) &&
                        ulong.TryParse(maxEpochStr, out maxEpoch))
                    {
                        Debug.Log("Loaded ephemeral key and nonce from PlayerPrefs");
                    }
                    else
                    {
                        // Generate new values as fallback
                        Debug.Log("No stored ephemeral key found. Generating new one.");

                        // Generate random ephemeral private key
                        byte[] ephemeralKeyBytes = NonceGenerator.RandomBytes();
                        ephemeralPrivateKey = Convert.ToBase64String(ephemeralKeyBytes);

                        // Get current epoch from Sui RPC
                        maxEpoch = await FetchCurrentEpochFromRPC() + 10; // Buffer of 10 epochs

                        // Generate randomness
                        randomness = NonceGenerator.GenerateRandomness();

                        // Save for later use
                        PlayerPrefs.SetString("ephemeralPrivateKey", ephemeralPrivateKey);
                        PlayerPrefs.SetString("maxEpoch", maxEpoch.ToString());
                        PlayerPrefs.SetString("randomness", randomness);
                        PlayerPrefs.Save();
                    }
                }

                Debug.Log($"Using ephemeral key (first 10 chars): {ephemeralPrivateKey.Substring(0, Math.Min(10, ephemeralPrivateKey.Length))}...");
                Debug.Log($"Using max epoch: {maxEpoch}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading/generating ephemeral key: {e}");
                OnError?.Invoke($"Key generation error: {e.Message}");
            }
        }

        public async Task GenerateZkProofAsync()
        {
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(ephemeralPrivateKey))
            {
                OnError?.Invoke("Cannot generate proof: Missing token or key");
                return;
            }

            try
            {
                // Generate the ZkLoginSignature with the proof
                zkSignature = await ZkProofGenerator.GenerateZkProofAsync(
                    accessToken,
                    userSalt,
                    ephemeralPrivateKey,
                    maxEpoch,
                    randomness
                );

                isProofGenerated = true;
                OnProofGenerated?.Invoke(suiAddress);
                Debug.Log($"ZK proof generated for address: {zkSignature}");
                Debug.Log("Successfully generated ZK proof");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating ZK proof: {e}");
                OnError?.Invoke($"Proof generation error: {e.Message}");
            }
        }

        // Keep your existing methods for signing transactions

        private string LoadOrCreateSalt(string userId)
        {
            // Check if we already have a salt for this user in PlayerPrefs
            string saltKey = $"sui_salt_{userId}";
            string salt = PlayerPrefs.GetString(saltKey, string.Empty);

            if (string.IsNullOrEmpty(salt))
            {
                // Generate a properly formatted salt (16 bytes as hex string)
                salt = GenerateSecureRandomSalt();

                // Store the salt
                PlayerPrefs.SetString(saltKey, salt);
                PlayerPrefs.Save();
                Debug.Log($"Created new salt for user: {salt}");
            }
            else
            {
                Debug.Log($"Loaded existing salt: {salt}");
            }

            return salt;
        }

        private string GenerateSecureRandomSalt()
        {
            // Generate exactly 16 bytes of random data
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }

            // Create BigInteger from the bytes (positive value)
            System.Numerics.BigInteger bigIntValue = new System.Numerics.BigInteger(saltBytes);
            bigIntValue = System.Numerics.BigInteger.Abs(bigIntValue); // Ensure positive

            // Convert to decimal string format which is what GenAddressSeed expects
            string decimalSalt = bigIntValue.ToString();

            Debug.Log($"Generated salt as decimal string: {decimalSalt}");
            return decimalSalt;
        }

        private async Task<ulong> FetchCurrentEpochFromRPC()
        {
            // Create a temporary client to fetch current epoch
            var client = new SuiRpcClient(rpcUrl);
            return await client.GetCurrentEpochAsync();
        }

        private void OnDestroy()
        {
            if (googleAuthManager != null)
            {
                googleAuthManager.OnAuthSuccess -= OnGoogleAuthSuccess;
                googleAuthManager.OnAuthError -= OnAuthErrorHandler;
            }
        }
    }
}