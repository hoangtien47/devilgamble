using Newtonsoft.Json;
using Sui.Cryptography.Ed25519;
using Sui.ZKLogin;
using Sui.ZKLogin.SDK;
using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZkLogin
{
    public static class ZkProofGenerator
    {
        private static readonly string PROVER_URL = "https://prover-dev.mystenlabs.com/v1";

        public static async Task<ZkLoginSignature> GenerateZkProofAsync(
            string jwt,
            string userSalt,
            string ephemeralPrivateKeyB64,
            ulong maxEpoch,
            string savedRandomness = null) // Add parameter for saved randomness
        {
            try
            {
                Debug.Log("Starting ZK proof generation process");

                // Decode the JWT to get necessary fields
                var decodedJwt = OpenDive.Utils.Jwt.JWTDecoder.DecodeJWT(jwt);
                if (decodedJwt == null)
                {
                    throw new Exception("Failed to decode JWT");
                }

                // Create the ephemeral key pair
                var ephemeralPrivateKey = new PrivateKey(ephemeralPrivateKeyB64);
                var ephemeralPublicKeyBase = ephemeralPrivateKey.PublicKey();

                // Create a proper Ed25519.PublicKey using the key data from the base class
                var ephemeralPublicKey = new PublicKey(ephemeralPublicKeyBase.KeyBase64);

                // Use saved randomness if provided, otherwise generate new randomness
                string randomness = savedRandomness;
                if (string.IsNullOrEmpty(randomness))
                {
                    randomness = NonceGenerator.GenerateRandomness();
                    Debug.Log("Generated new randomness for proof");
                }
                else
                {
                    Debug.Log("Using provided randomness from authentication");
                }

                // Generate nonce with the randomness
                string nonce = NonceGenerator.GenerateNonce(
                    ephemeralPublicKey,
                    (int)maxEpoch,
                    randomness
                );

                // Calculate the address seed
                var addressSeed = Utils.GenAddressSeed(
                    userSalt,
                    "sub", // Using "sub" as the key claim
                    decodedJwt.Payload.Sub,
                    decodedJwt.Payload.Aud
                );

                // Create a proof request to send to the prover service
                var proofRequest = new ZkProofRequest
                {
                    Jwt = jwt,
                    AddressSeed = addressSeed.ToString(),
                    Salt = userSalt,
                    KeyClaimName = "sub", // Add the key claim name (typically "sub" for subject)
                    MaxEpoch = maxEpoch,
                    JwtRandomness = randomness, // Use the consistent randomness
                    EphemeralPublicKey = ephemeralPublicKeyBase.KeyBase64,
                    ExtendedEphemeralPublicKey = $"PK${ephemeralPublicKeyBase.KeyBase64}"
                };

                // Log the complete request for debugging
                string requestJson = JsonConvert.SerializeObject(proofRequest, Formatting.Indented);
                Debug.Log($"Sending full request to prover: {requestJson}");

                // Call the prover service to get the proof
                var proofResponse = await SendProofRequestAsync(proofRequest);

                // Create the ZK Login signature components
                var zkLoginSignature = new ZkLoginSignature();

                // Populate the ZkLoginSignature with the proof data
                zkLoginSignature.Inputs = new Inputs
                {
                    ProofPoints = proofResponse.ProofPoints,
                    IssBase64Details = proofResponse.IssBase64Details,
                    HeaderBase64 = proofResponse.HeaderBase64,
                    AddressSeed = addressSeed.ToString()
                };

                zkLoginSignature.MaxEpoch = maxEpoch;

                // Sign with the ephemeral private key and convert signature to byte array
                var signature = ephemeralPrivateKey.Sign(proofResponse.DataToSign);
                zkLoginSignature.UserSignature = signature.SignatureBytes;

                Debug.Log("Successfully generated ZK proof");
                return zkLoginSignature;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating ZK proof: {ex.Message}");
                throw;
            }
        }

        // Rest of the class remains unchanged
        private static async Task<ZkProofResponse> SendProofRequestAsync(ZkProofRequest request)
        {
            // Create HTTP client to call the prover service
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                // Use Newtonsoft.Json for serialization - it handles property names better than JsonUtility
                string jsonRequest = JsonConvert.SerializeObject(
                    request,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    }
                );

                Debug.Log($"Sending request to prover: {jsonRequest}");

                // Set up the request with proper content type
                var content = new System.Net.Http.StringContent(
                    jsonRequest,
                    Encoding.UTF8,
                    "application/json"
                );

                // Add recommended headers
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                // Call the prover service
                var response = await httpClient.PostAsync(PROVER_URL, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Prover service error ({response.StatusCode}): {responseContent}");
                    throw new Exception($"Prover service error ({response.StatusCode}): {responseContent}");
                }

                Debug.Log($"Received prover response: {responseContent}");

                // Parse the response using Newtonsoft.Json
                return JsonConvert.DeserializeObject<ZkProofResponse>(responseContent);
            }
        }
    }
}

[Serializable]
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ZkProofRequest
{
    [JsonProperty("jwt")]
    public string Jwt;

    [JsonProperty("addressSeed")]
    public string AddressSeed;

    [JsonProperty("salt")]
    public string Salt;

    [JsonProperty("keyClaimName")]
    public string KeyClaimName;

    [JsonProperty("maxEpoch")]
    public ulong MaxEpoch;

    [JsonProperty("jwtRandomness")]
    public string JwtRandomness;

    [JsonProperty("ephemeralPublicKey")]
    public string EphemeralPublicKey;

    [JsonProperty("extendedEphemeralPublicKey")]
    public string ExtendedEphemeralPublicKey;
}

[Serializable]
public class ZkProofResponse
{
    [JsonProperty("proofPoints")]
    public ProofPoints ProofPoints;

    [JsonProperty("issBase64Details")]
    public ZkLoginSignatureInputsClaim IssBase64Details;

    [JsonProperty("headerBase64")]
    public BigInteger HeaderBase64;

    [JsonProperty("dataToSign")]
    public byte[] DataToSign;
}
