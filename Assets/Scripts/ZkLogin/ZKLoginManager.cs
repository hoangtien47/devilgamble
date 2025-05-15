//using Cdm.Authentication.Clients;
//using Newtonsoft.Json;
//using Sui.Rpc.Client;
//using System;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.UI;



//public class ZKLoginManager : MonoBehaviour
//{
//    [Header("Google OAuth Configuration")]
//    [SerializeField] private string clientId = "YOUR_GOOGLE_CLIENT_ID";
//    [SerializeField] private string redirectUri = "http://localhost";

//    [Header("UI References")]
//    [SerializeField] private Button loginButton;
//    [SerializeField] private Button logoutButton;
//    [SerializeField] private Text statusText;
//    [SerializeField] private Text addressText;

//    [Header("Network")]
//    [SerializeField] private bool useTestnet = true;

//    // Internal components
//    private SuiClient _client;
//    private ZKLoginHandler _zkLogin;
//    private GoogleAuth _googleAuth;
//    private ZKLoginAccount _currentAccount;

//    // State tracking
//    private string _ephemeralPrivateKey;
//    private ulong _maxEpoch;
//    private bool _isLoggedIn = false;

//    void Start()
//    {
//        // Initialize Sui components
//        _zkLogin = new ZKLoginHandler();
//        _client = new SuiClient(useTestnet ? Constants.TestnetConnection : Constants.MainnetConnection);

//        // Initialize Google OAuth provider with required scopes for ZK Login
//        _googleAuth = new GoogleAuth(
//            clientId,
//            "", // No client secret needed for client-side flow
//            redirectUri,
//            new string[] { "openid", "email", "profile" }
//        );

//        // Set up UI
//        loginButton.onClick.AddListener(StartLogin);
//        logoutButton.onClick.AddListener(Logout);
//        logoutButton.gameObject.SetActive(false);
//        UpdateUI();

//        // Check for existing session
//        TryRestoreSession();
//    }

//    public async void StartLogin()
//    {
//        statusText.text = "Initializing login...";

//        try
//        {
//            // Generate ephemeral key pair
//            var ephemeralKeyPair = _zkLogin.GenerateEphemeralKeyPair();
//            _ephemeralPrivateKey = ephemeralKeyPair.PrivateKey;

//            // Get current epoch from network
//            var epochResponse = await _client.GetCurrentEpochAsync();
//            if (!epochResponse.IsSuccess)
//            {
//                Debug.LogError($"Failed to get current epoch: {epochResponse.Error?.Message}");
//                statusText.text = "Failed to initialize login. Network error.";
//                return;
//            }

//            // Set max epoch (current + 10 for safety)
//            _maxEpoch = epochResponse.Result.Epoch + 10;

//            // Generate nonce using ephemeral public key and max epoch
//            string nonce = _zkLogin.GenerateNonce(ephemeralKeyPair.PublicKey, _maxEpoch);

//            // Store ephemeral key for later use
//            PlayerPrefs.SetString("ephemeralPrivateKey", _ephemeralPrivateKey);

//            // Set nonce for OAuth flow
//            _googleAuth.AdditionalParameters = new System.Collections.Generic.Dictionary<string, string> {
//                { "nonce", nonce }
//            };

//            // Start Google sign-in flow
//            statusText.text = "Opening Google login...";
//            _googleAuth.SignIn(OnAuthSuccess, OnAuthFailure);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error starting login: {ex.Message}");
//            statusText.text = "Error starting login. See logs for details.";
//        }
//    }

//    private async void OnAuthSuccess(IAuthenticationResult result)
//    {
//        Debug.Log("Google authentication successful");
//        statusText.text = "Google login successful, processing...";

//        try
//        {
//            // Extract JWT (ID token) from the authentication result
//            string jwt = result.IdToken;

//            if (string.IsNullOrEmpty(jwt))
//            {
//                Debug.LogError("No ID token received from Google");
//                statusText.text = "Error: No ID token received";
//                return;
//            }

//            // Process the JWT for ZK Login
//            await ProcessJwt(jwt);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error processing Google auth: {ex.Message}");
//            statusText.text = "Error processing Google login.";
//        }
//    }

//    private void OnAuthFailure(string errorMessage)
//    {
//        Debug.LogError($"Google authentication failed: {errorMessage}");
//        statusText.text = $"Login failed: {errorMessage}";
//    }

//    private async Task ProcessJwt(string jwt)
//    {
//        statusText.text = "Processing Google authentication...";

//        try
//        {
//            // Get the stored ephemeral key
//            string ephemeralKey = PlayerPrefs.GetString("ephemeralPrivateKey");

//            if (string.IsNullOrEmpty(ephemeralKey))
//            {
//                throw new Exception("No ephemeral key found. Please try logging in again.");
//            }

//            // Parse JWT data
//            var jwtData = _zkLogin.ParseJwt(jwt);

//            // Get or create user salt
//            string userSalt = GetOrCreateUserSalt();

//            statusText.text = "Generating ZK proof...";

//            // Generate ZK proof
//            var zkProof = await _zkLogin.GenerateZkProofAsync(jwt, userSalt);

//            // Create ZK Login account
//            _currentAccount = new ZKLoginAccount(
//                zkProof,
//                ephemeralKey,
//                jwtData.Sub,      // Subject (user ID)
//                jwtData.Iss,      // Issuer
//                clientId,         // Audience (for Google, this is the client ID)
//                userSalt
//            );

//            // Save account info for session persistence
//            SaveSession(_currentAccount, jwt, ephemeralKey);

//            // Update UI
//            _isLoggedIn = true;
//            UpdateUI();

//            statusText.text = "Login successful!";

//            // Log the address for debugging
//            Debug.Log($"Successfully logged in with ZK Login. Address: {_currentAccount.SuiAddress()}");
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error processing JWT: {ex.Message}");
//            statusText.text = $"Authentication error: {ex.Message}";
//        }
//    }

//    public void Logout()
//    {
//        // Sign out from Google
//        _googleAuth.SignOut();

//        // Clear session data
//        PlayerPrefs.DeleteKey("zkLoginSession");
//        _currentAccount = null;
//        _isLoggedIn = false;

//        // Update UI
//        UpdateUI();
//        statusText.text = "Logged out successfully";
//    }

//    private void TryRestoreSession()
//    {
//        try
//        {
//            string sessionData = PlayerPrefs.GetString("zkLoginSession", "");
//            if (string.IsNullOrEmpty(sessionData))
//                return;

//            var session = JsonConvert.DeserializeObject<ZKLoginSession>(sessionData);
//            if (session == null || IsSessionExpired(session))
//            {
//                Logout();
//                return;
//            }

//            // Recreate account
//            _currentAccount = new ZKLoginAccount(
//                session.ZkProof,
//                session.EphemeralKey,
//                session.Sub,
//                session.Iss,
//                session.Aud,
//                session.UserSalt
//            );

//            _isLoggedIn = true;
//            UpdateUI();
//            statusText.text = "Session restored";
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error restoring session: {ex.Message}");
//            Logout();
//        }
//    }

//    private void SaveSession(ZKLoginAccount account, string jwt, string ephemeralKey)
//    {
//        try
//        {
//            var jwtData = _zkLogin.ParseJwt(jwt);

//            // Create session object
//            var session = new ZKLoginSession
//            {
//                ZkProof = account.GetZkProof(),
//                EphemeralKey = ephemeralKey,
//                Sub = jwtData.Sub,
//                Iss = jwtData.Iss,
//                Aud = clientId,
//                UserSalt = account.GetSalt(),
//                ExpiryTime = DateTime.UtcNow.AddHours(1) // Session expiry (1 hour)
//            };

//            // Save to PlayerPrefs
//            string sessionJson = JsonConvert.SerializeObject(session);
//            PlayerPrefs.SetString("zkLoginSession", sessionJson);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error saving session: {ex.Message}");
//        }
//    }

//    private bool IsSessionExpired(ZKLoginSession session)
//    {
//        return DateTime.UtcNow > session.ExpiryTime;
//    }

//    private string GetOrCreateUserSalt()
//    {
//        string salt = PlayerPrefs.GetString("userSalt", "");

//        if (string.IsNullOrEmpty(salt))
//        {
//            salt = _zkLogin.GenerateRandomSalt();
//            PlayerPrefs.SetString("userSalt", salt);
//        }

//        return salt;
//    }

//    private void UpdateUI()
//    {
//        loginButton.gameObject.SetActive(!_isLoggedIn);
//        logoutButton.gameObject.SetActive(_isLoggedIn);

//        if (_isLoggedIn && _currentAccount != null)
//        {
//            addressText.text = $"Address: {_currentAccount.SuiAddress()}";
//        }
//        else
//        {
//            addressText.text = "Not logged in";
//        }
//    }

//    // Example method to execute a transaction with the ZK Login account
//    public async Task<bool> ExecuteTransaction(TransactionBlock tx)
//    {
//        if (!_isLoggedIn || _currentAccount == null)
//        {
//            statusText.text = "Please log in first";
//            return false;
//        }

//        try
//        {
//            statusText.text = "Executing transaction...";
//            var result = await _client.SignAndExecuteTransactionBlockAsync(tx, _currentAccount);

//            if (result.IsSuccess)
//            {
//                statusText.text = "Transaction successful!";
//                return true;
//            }
//            else
//            {
//                statusText.text = $"Transaction failed: {result.Error?.Message}";
//                return false;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Transaction error: {ex.Message}");
//            statusText.text = "Transaction error. See logs for details.";
//            return false;
//        }
//    }
//}

//// Class to store session data
//[Serializable]
//public class ZKLoginSession
//{
//    public string ZkProof { get; set; }
//    public string EphemeralKey { get; set; }
//    public string Sub { get; set; }
//    public string Iss { get; set; }
//    public string Aud { get; set; }
//    public string UserSalt { get; set; }
//    public DateTime ExpiryTime { get; set; }
//}