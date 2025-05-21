//using Sui.Cryptography;
//using Sui.ZKLogin;
//using Sui.ZKLogin.SDK;
//using System;
//using System.Numerics;
//using System.Threading.Tasks;
//using Unity.Services.Authentication;
//using Unity.Services.Authentication.PlayerAccounts;
//using Unity.Services.Core;
//using UnityEngine;
//using ZkLogin;

//public class LoginManager : MonoBehaviour
//{
//    [SerializeField] private string rpcUrl = "https://fullnode.devnet.sui.io";

//    public event Action<PlayerInfo, string, string, string> OnSignIn;
//    public event Action<string> OnProofGenerated;
//    public event Action<string> OnError;

//    private PlayerInfo playerInfo;
//    private string suiAddress;
//    private string userSalt;
//    private string accessToken;
//    private string ephemeralPrivateKey;
//    private ulong maxEpoch;

//    // Keep track of ZKLogin state
//    private bool isProofGenerated = false;
//    private ZkLoginSignature zkSignature;

//    async void Awake()
//    {
//        // Initialize Unity Services
//        await UnityServices.InitializeAsync();
//        PlayerAccountService.Instance.SignedIn += OnSignedIn;
//    }

//    private async void OnSignedIn()
//    {
//        try
//        {
//            accessToken = PlayerAccountService.Instance.AccessToken;
//            await SignInWithUnityAsync(accessToken);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Error signing in with Unity: {e}");
//            OnError?.Invoke($"Sign-in error: {e.Message}");
//        }
//    }

//    public async Task InitSignIn()
//    {
//        await PlayerAccountService.Instance.StartSignInAsync();
//    }

//    private async Task SignInWithUnityAsync(string accessToken)
//    {
//        try
//        {
//            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
//            Debug.Log("Successfully signed in with Unity.");

//            playerInfo = AuthenticationService.Instance.PlayerInfo;
//            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

//            // Generate a Sui ZKLogin address from the JWT token
//            await GenerateSuiAddressFromJwt(accessToken);

//            OnSignIn?.Invoke(playerInfo, name, accessToken, suiAddress);
//        }
//        catch (AuthenticationException e)
//        {
//            Debug.LogError($"Error signing in with Unity: {e}");
//            OnError?.Invoke($"Authentication error: {e.Message}");
//        }
//        catch (RequestFailedException e)
//        {
//            Debug.LogError($"Unexpected error: {e}");
//            OnError?.Invoke($"Request failed: {e.Message}");
//        }
//    }

//    private async Task GenerateSuiAddressFromJwt(string jwtToken)
//    {
//        try
//        {
//            // Normalize the JWT token to fix array 'aud' issue
//            string normalizedJwt = CustomJWTDecoder.NormalizeJwt(jwtToken);

//            // Decode the JWT payload for information only
//            var payload = CustomJWTDecoder.DecodePayload(normalizedJwt);
//            Debug.Log($"Using JWT with issuer: {payload.iss}, subject: {payload.sub}");

//            // Check if we have a stored salt for this user or generate a new one
//            userSalt = LoadOrCreateSalt(playerInfo.Id);

//            // Generate a Sui address using the normalized JWT
//            suiAddress = Sui.ZKLogin.SDK.Address.JwtToAddress(normalizedJwt, userSalt);
//            Debug.Log($"Generated Sui address: {suiAddress}");

//            // Generate ephemeral key pair for transaction signing
//            await GenerateEphemeralKeyAndNonce();
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Error generating Sui address: {e}");
//            OnError?.Invoke($"Address generation error: {e.Message}");
//        }
//    }

//    private async Task GenerateEphemeralKeyAndNonce()
//    {
//        try
//        {
//            // Generate random ephemeral private key
//            byte[] ephemeralKeyBytes = NonceGenerator.RandomBytes();
//            ephemeralPrivateKey = Convert.ToBase64String(ephemeralKeyBytes);

//            // Fetch current epoch from Sui RPC
//            maxEpoch = await FetchCurrentEpochFromRPC() + 10; // Add buffer of 10 epochs

//            Debug.Log($"Generated ephemeral key and max epoch: {maxEpoch}");

//            // Store for later use
//            PlayerPrefs.SetString($"ephemeralKey_{playerInfo.Id}", ephemeralPrivateKey);
//            PlayerPrefs.Save();
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Error generating ephemeral key: {e}");
//            OnError?.Invoke($"Key generation error: {e.Message}");
//        }
//    }

//    public async Task GenerateZkProofAsync()
//    {
//        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(ephemeralPrivateKey))
//        {
//            OnError?.Invoke("Cannot generate proof: Missing token or key");
//            return;
//        }

//        try
//        {
//            // Get normalized JWT
//            string normalizedJwt = CustomJWTDecoder.NormalizeJwt(accessToken);

//            // Create the ZkLoginSignature with the proof
//            zkSignature = await ZkProofGenerator.GenerateZkProofAsync(
//                normalizedJwt,
//                userSalt,
//                ephemeralPrivateKey,
//                maxEpoch
//            );

//            isProofGenerated = true;
//            OnProofGenerated?.Invoke(suiAddress);
//            Debug.Log("Successfully generated ZK proof");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Error generating ZK proof: {e}");
//            OnError?.Invoke($"Proof generation error: {e.Message}");
//        }
//    }

//    public async Task<string> SignAndExecuteTransaction(string transactionData)
//    {
//        if (!isProofGenerated || zkSignature == null)
//        {
//            OnError?.Invoke("Cannot sign transaction: ZK proof not generated");
//            return null;
//        }

//        try
//        {
//            // Create a Sui RPC client
//            var client = new SuiRpcClient(rpcUrl);

//            // Sign and execute transaction
//            string txBytes = transactionData;
//            string signature = SerializeZkLoginSignature(zkSignature);
//            string txResult = await client.ExecuteTransactionAsync(txBytes, signature, suiAddress);

//            return txResult;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Error executing transaction: {e}");
//            OnError?.Invoke($"Transaction error: {e.Message}");
//            return null;
//        }
//    }

//    private string SerializeZkLoginSignature(ZkLoginSignature sig)
//    {
//        // Serialize the ZkLoginSignature for transaction submission
//        // Implementation depends on the exact format required by Sui RPC
//        // This is typically Base64 encoding of the signature bytes with scheme prefix
//        byte[] sigBytes = ZkLoginSignature.GetZkLoginSignatureBytes(
//            sig.Inputs,
//            sig.MaxEpoch,
//            sig.UserSignature
//        );

//        // Add ZkLogin signature scheme flag
//        byte[] fullSigBytes = new byte[sigBytes.Length + 1];
//        fullSigBytes[0] = SignatureSchemeToFlag.ZkLogin;
//        Buffer.BlockCopy(sigBytes, 0, fullSigBytes, 1, sigBytes.Length);

//        return Convert.ToBase64String(fullSigBytes);
//    }

//    private string LoadOrCreateSalt(string userId)
//    {
//        // Check if we already have a salt for this user in PlayerPrefs
//        string saltKey = $"sui_salt_{userId}";
//        string salt = PlayerPrefs.GetString(saltKey, string.Empty);

//        if (string.IsNullOrEmpty(salt))
//        {
//            // Generate a random salt
//            salt = GenerateSecureRandomSalt();

//            // Store the salt
//            PlayerPrefs.SetString(saltKey, salt);
//            PlayerPrefs.Save();
//            Debug.Log($"Created new salt for user: {salt}");
//        }
//        else
//        {
//            Debug.Log($"Loaded existing salt: {salt}");
//        }

//        return salt;
//    }

//    private string GenerateSecureRandomSalt()
//    {
//        // Generate a secure random number for the salt
//        System.Random random = new System.Random();
//        byte[] randomBytes = new byte[16];
//        random.NextBytes(randomBytes);

//        // Convert to BigInteger and take absolute value to ensure positive number
//        BigInteger randomBigInt = new BigInteger(randomBytes);
//        randomBigInt = BigInteger.Abs(randomBigInt);

//        return randomBigInt.ToString();
//    }

//    private async Task<ulong> FetchCurrentEpochFromRPC()
//    {
//        // Create a temporary client to fetch current epoch
//        var client = new SuiRpcClient(rpcUrl);
//        return await client.GetCurrentEpochAsync();
//    }

//    private void OnDestroy()
//    {
//        if (PlayerAccountService.Instance != null)
//        {
//            PlayerAccountService.Instance.SignedIn -= OnSignedIn;
//        }
//    }
//}