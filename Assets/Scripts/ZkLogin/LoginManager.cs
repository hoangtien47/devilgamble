//using Sui.ZKLogin;
//using Sui.ZKLogin.SDK;
//using System;
//using System.Collections.Generic;
//using System.Security.Cryptography;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace ZkLogin
//{
//    public class LoginManager : MonoBehaviour
//    {
//        [SerializeField] private string rpcUrl = "https://fullnode.devnet.sui.io";
//        SuiUnityWrapper suiWrapper;

//        [SerializeField] private GoogleAuthManager googleAuthManager;
//        [SerializeField] private GoogleAuthConfig authConfig;

//        public event Action<string, string, string> OnSignIn; // username, access token, address
//        public event Action<string> OnProofGenerated;
//        public event Action<string> OnError;

//        private string username;
//        private string suiAddress;
//        private string userSalt;
//        private string accessToken;
//        private string ephemeralPrivateKey;
//        private ulong maxEpoch;
//        private string randomness;

//        // State
//        private bool isProofGenerated = false;
//        private ZkLoginSignature zkSignature;

//        private void Start()
//        {
//            // Create GoogleAuthManager if not already assigned
//            if (googleAuthManager == null)
//            {
//                var existingManager = FindObjectOfType<GoogleAuthManager>();
//                suiWrapper = FindObjectOfType<SuiUnityWrapper>();

//                if (existingManager != null)
//                {
//                    googleAuthManager = existingManager;
//                }
//                else
//                {
//                    GameObject authObj = new GameObject("GoogleAuthManager");
//                    googleAuthManager = authObj.AddComponent<GoogleAuthManager>();
//                    if (authConfig != null)
//                    {
//                        // Set config through reflection since it's a SerializeField
//                        var configField = typeof(GoogleAuthManager).GetField("config",
//                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
//                        if (configField != null) configField.SetValue(googleAuthManager, authConfig);
//                    }
//                }
//            }

//            // Subscribe to Google auth events
//            googleAuthManager.OnAuthSuccess += OnGoogleAuthSuccess;
//            googleAuthManager.OnAuthError += OnAuthErrorHandler;
//        }

//        public async Task InitSignIn()
//        {
//            try
//            {
//                // Start Google authentication flow
//                accessToken = await googleAuthManager.AuthenticateWithGoogleAsync();
//                Debug.Log("Successfully received Google ID token");

//                // Parse JWT to get user info
//                var payload = CustomJWTDecoder.DecodePayload(accessToken);
//                username = payload.name ?? payload.email ?? "User";

//                // Generate Sui address
//                await GenerateSuiAddressFromJwt(accessToken);

//                OnSignIn?.Invoke(username, accessToken, suiAddress);
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"Error signing in with Google: {e}");
//                OnError?.Invoke($"Sign-in error: {e.Message}");
//            }
//        }

//        private void OnGoogleAuthSuccess(string token)
//        {
//            // Store token for later use
//            accessToken = token;
//            Debug.Log("Google authentication successful");

//            // Parse the token to get user information if needed
//            try
//            {
//                var payload = CustomJWTDecoder.DecodePayload(token);
//                username = payload.name ?? payload.email ?? "User";
//                Debug.Log($"Logged in as: {username}");
//            }
//            catch (Exception e)
//            {
//                Debug.LogWarning($"Could not parse JWT payload: {e.Message}");
//            }
//        }

//        private void OnAuthErrorHandler(string errorMsg)
//        {
//            OnError?.Invoke(errorMsg);
//        }

//        private async Task GenerateSuiAddressFromJwt(string jwtToken)
//        {
//            try
//            {
//                var payload = CustomJWTDecoder.DecodePayload(jwtToken);
//                Debug.Log($"Using JWT with issuer: {payload.iss}, subject: {payload.sub}");

//                // Load or create salt for this user
//                userSalt = LoadOrCreateSalt(payload.sub);

//                // Generate Sui address using the JWT
//                suiAddress = Sui.ZKLogin.SDK.Address.JwtToAddress(jwtToken, userSalt);
//                Debug.Log($"Generated Sui address: {suiAddress}");

//                string result = suiWrapper.Invoke("jwtToAddress", jwtToken, userSalt);
//                Debug.Log($"Sui address from wrapper: {result}");

//                // Use the previously generated key or create a new one
//                await LoadOrGenerateEphemeralKeyAndNonce();
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"Error generating Sui address: {e}");
//                OnError?.Invoke($"Address generation error: {e.Message}");
//            }
//        }

//        private async Task LoadOrGenerateEphemeralKeyAndNonce()
//        {
//            try
//            {
//                // Try to get ephemeral key directly from GoogleAuthManager if available
//                if (googleAuthManager != null && !string.IsNullOrEmpty(googleAuthManager.EphemeralPrivateKey))
//                {
//                    ephemeralPrivateKey = googleAuthManager.EphemeralPrivateKey;
//                    maxEpoch = googleAuthManager.MaxEpoch;
//                    randomness = googleAuthManager.Randomness;

//                    Debug.Log("Using ephemeral key and nonce from GoogleAuthManager");
//                }
//                else
//                {
//                    // Try to load from PlayerPrefs
//                    ephemeralPrivateKey = PlayerPrefs.GetString("ephemeralPrivateKey", string.Empty);
//                    string maxEpochStr = PlayerPrefs.GetString("maxEpoch", string.Empty);
//                    randomness = PlayerPrefs.GetString("randomness", string.Empty);

//                    // Check if we found valid values
//                    if (!string.IsNullOrEmpty(ephemeralPrivateKey) && !string.IsNullOrEmpty(maxEpochStr) &&
//                        ulong.TryParse(maxEpochStr, out maxEpoch))
//                    {
//                        Debug.Log("Loaded ephemeral key and nonce from PlayerPrefs");
//                    }
//                    else
//                    {
//                        // Generate new values as fallback
//                        Debug.Log("No stored ephemeral key found. Generating new one.");

//                        // Generate random ephemeral private key
//                        byte[] ephemeralKeyBytes = NonceGenerator.RandomBytes();
//                        ephemeralPrivateKey = Convert.ToBase64String(ephemeralKeyBytes);

//                        // Get current epoch from Sui RPC
//                        maxEpoch = await FetchCurrentEpochFromRPC() + 10; // Buffer of 10 epochs

//                        // Generate randomness
//                        randomness = NonceGenerator.GenerateRandomness();

//                        // Save for later use
//                        PlayerPrefs.SetString("ephemeralPrivateKey", ephemeralPrivateKey);
//                        PlayerPrefs.SetString("maxEpoch", maxEpoch.ToString());
//                        PlayerPrefs.SetString("randomness", randomness);
//                        PlayerPrefs.Save();
//                    }
//                }

//                Debug.Log($"Using ephemeral key (first 10 chars): {ephemeralPrivateKey.Substring(0, Math.Min(10, ephemeralPrivateKey.Length))}...");
//                Debug.Log($"Using max epoch: {maxEpoch}");
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"Error loading/generating ephemeral key: {e}");
//                OnError?.Invoke($"Key generation error: {e.Message}");
//            }
//        }

//        public async Task GenerateZkProofAsync()
//        {
//            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(ephemeralPrivateKey))
//            {
//                OnError?.Invoke("Cannot generate proof: Missing token or key");
//                return;
//            }

//            try
//            {
//                // Generate the ZkLoginSignature with the proof
//                zkSignature = await ZkProofGenerator.GenerateZkProofAsync(
//                    accessToken,
//                    userSalt,
//                    ephemeralPrivateKey,
//                    maxEpoch,
//                    randomness
//                );

//                isProofGenerated = true;
//                OnProofGenerated?.Invoke(suiAddress);
//                Debug.Log($"ZK proof generated for address: {zkSignature}");
//                Debug.Log("Successfully generated ZK proof");
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"Error generating ZK proof: {e}");
//                OnError?.Invoke($"Proof generation error: {e.Message}");
//            }
//        }

//        // Keep your existing methods for signing transactions

//        private string LoadOrCreateSalt(string userId)
//        {
//            // Check if we already have a salt for this user in PlayerPrefs
//            string saltKey = $"sui_salt_{userId}";
//            string salt = PlayerPrefs.GetString(saltKey, string.Empty);

//            if (string.IsNullOrEmpty(salt))
//            {
//                // Generate a properly formatted salt (16 bytes as hex string)
//                salt = GenerateSecureRandomSalt();

//                // Store the salt
//                PlayerPrefs.SetString(saltKey, salt);
//                PlayerPrefs.Save();
//                Debug.Log($"Created new salt for user: {salt}");
//            }
//            else
//            {
//                Debug.Log($"Loaded existing salt: {salt}");
//            }

//            return salt;
//        }

//        private string GenerateSecureRandomSalt()
//        {
//            // Generate exactly 16 bytes of random data
//            byte[] saltBytes = new byte[16];
//            using (var rng = new RNGCryptoServiceProvider())
//            {
//                rng.GetBytes(saltBytes);
//            }

//            // Create BigInteger from the bytes (positive value)
//            System.Numerics.BigInteger bigIntValue = new System.Numerics.BigInteger(saltBytes);
//            bigIntValue = System.Numerics.BigInteger.Abs(bigIntValue); // Ensure positive

//            // Convert to decimal string format which is what GenAddressSeed expects
//            string decimalSalt = bigIntValue.ToString();

//            Debug.Log($"Generated salt as decimal string: {decimalSalt}");
//            return decimalSalt;
//        }

//        private async Task<ulong> FetchCurrentEpochFromRPC()
//        {
//            // Create a temporary client to fetch current epoch
//            var client = new SuiRpcClient(rpcUrl);
//            return await client.GetCurrentEpochAsync();
//        }


//        public async Task<string> ExecuteTransactionAsync(string packageId, string moduleName, string functionName,
//           List<object> arguments, ulong gasBudget = 10000000)
//        {
//            if (!isProofGenerated || zkSignature == null)
//            {
//                OnError?.Invoke("Cannot execute transaction: ZK proof not generated");
//                return null;
//            }

//            try
//            {
//                Debug.Log($"Creating transaction to call {moduleName}::{functionName}");

//                // 1. Create a Connection object first, then use it to create SuiClient
//                var connection = new Sui.Rpc.Connection(rpcUrl);
//                var client = new Sui.Rpc.Client.SuiClient(connection);

//                // 2. Create transaction block
//                var txBlock = new Sui.Transactions.TransactionBlock();

//                // 3. Set gas budget for the transaction
//                txBlock.SetGasBudget(gasBudget);

//                // 4. Set the sender address
//                var senderAddress = Sui.Accounts.AccountAddress.FromHex(suiAddress);
//                txBlock.SetSenderIfNotSet(senderAddress);

//                Debug.Log($"Transaction sender: {senderAddress}");

//                // 5. Prepare arguments for the move call
//                List<Sui.Transactions.TransactionArgument> txArgs = new List<Sui.Transactions.TransactionArgument>();
//                foreach (var arg in arguments)
//                {
//                    if (arg is string && ((string)arg).StartsWith("0x"))
//                    {
//                        // This is an object ID argument - use AddObjectInput
//                        txArgs.Add(txBlock.AddObjectInput((string)arg));
//                    }
//                    else
//                    {
//                        // This is a pure value argument - use AddPure
//                        if (arg is int intValue)
//                        {
//                            txArgs.Add(txBlock.AddPure(new OpenDive.BCS.U64((ulong)intValue)));
//                        }
//                        else if (arg is string strValue)
//                        {
//                            txArgs.Add(txBlock.AddPure(new OpenDive.BCS.BString(strValue)));
//                        }
//                        else if (arg is bool boolValue)
//                        {
//                            txArgs.Add(txBlock.AddPure(new OpenDive.BCS.Bool(boolValue)));
//                        }
//                        // Add other type conversions as needed
//                    }
//                }

//                Debug.Log($"Transaction arguments: {txArgs}");

//                // 6. Create a proper SuiMoveNormalizedStructType using FromStr method
//                // Format: "0xPACKAGE_ID::MODULE_NAME::FUNCTION_NAME"
//                string fullPath = $"{packageId}::{moduleName}::{functionName}";
//                var targetType = Sui.Types.SuiMoveNormalizedStructType.FromStr(fullPath);

//                // Create move call with the target
//                var moveCall = new Sui.Transactions.MoveCall(
//                    targetType,
//                    null,  // No type arguments
//                    txArgs.ToArray()
//                );

//                Debug.Log($"moveCall kind: {moveCall}");

//                // Create command and add to transaction
//                var command = new Sui.Transactions.Command(
//                    Sui.Transactions.CommandKind.MoveCall,
//                    moveCall
//                );

//                Debug.Log($"Transaction command: {command}");

//                txBlock.AddTransaction(command);

//                // 7. Build the transaction
//                var buildOptions = new Sui.Transactions.BuildOptions(client);
//                byte[] txBytes = await txBlock.Build(buildOptions);

//                if (txBlock.Error != null)
//                {
//                    OnError?.Invoke($"Transaction build error: {txBlock.Error.Message}");
//                    return null;
//                }

//                // 8. Get serialized ZkLoginSignature
//                string serializedSignature = ZkLoginSignature.GetZkLoginSignature(
//                    zkSignature.Inputs,
//                    zkSignature.MaxEpoch,
//                    zkSignature.UserSignature
//                );

//                Debug.Log($"Serialized ZK signature: {serializedSignature.Substring(0, Math.Min(20, serializedSignature.Length))}...");

//                // 9. Execute the transaction
//                var response = await client.ExecuteTransactionBlockAsync(
//                    txBytes,
//                    new List<string> { serializedSignature },
//                    new Sui.Rpc.Models.TransactionBlockResponseOptions(
//                        showEffects: true,
//                        showEvents: true,
//                        showObjectChanges: true
//                    ),
//                    Sui.Rpc.Models.RequestType.WaitForLocalExecution
//                );

//                // 10. Process and return the result
//                if (response.Error != null)
//                {
//                    string errorMsg = $"Transaction error: {response.Error.Message}";
//                    Debug.LogError(errorMsg);
//                    OnError?.Invoke(errorMsg);
//                    return null;
//                }

//                Debug.Log($"Transaction successful! Digest: {response.Result.Digest}");
//                return response.Result.Digest;
//            }
//            catch (System.Exception e)
//            {
//                string errorMsg = $"Failed to execute transaction: {e.Message}";
//                Debug.LogError(errorMsg);
//                OnError?.Invoke(errorMsg);
//                return null;
//            }
//        }

//        private void OnDestroy()
//        {
//            if (googleAuthManager != null)
//            {
//                googleAuthManager.OnAuthSuccess -= OnGoogleAuthSuccess;
//                googleAuthManager.OnAuthError -= OnAuthErrorHandler;
//            }
//        }
//    }
//}