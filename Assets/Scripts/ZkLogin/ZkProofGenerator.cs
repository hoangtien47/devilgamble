using Newtonsoft.Json;
using OpenDive.BCS;
using Sui.Cryptography.Ed25519;
using Sui.ZKLogin;
using Sui.ZKLogin.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
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
                Debug.Log($"Zk Private key: {ephemeralPrivateKey}");
                Debug.Log($"Zk Public key: {ephemeralPublicKey}");
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
                Debug.Log($"ZK Generated nonce for proof: {nonce}");

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
                    //AddressSeed = addressSeed.ToString(),
                    Salt = userSalt,
                    KeyClaimName = "sub", // Add the key claim name (typically "sub" for subject)
                    MaxEpoch = maxEpoch,
                    JwtRandomness = randomness, // Use the consistent randomness
                    //EphemeralPublicKey = ephemeralPublicKeyBase.KeyBase64,
                    ExtendedEphemeralPublicKey = ephemeralPublicKeyBase.KeyBase64
                };

                // Log the complete request for debugging
                string requestJson = JsonConvert.SerializeObject(proofRequest, Formatting.Indented);
                Debug.Log($"Sending full request to prover: {requestJson}");

                // Call the prover service to get the proof
                var proofResponse = await SendProofRequestAsync(proofRequest);

                Debug.Log($"2Received proof response: {JsonConvert.SerializeObject(proofResponse, Formatting.Indented)}");

                // Create the ZK Login signature components
                var zkLoginSignature = new ZkLoginSignature();

                // Populate the ZkLoginSignature with the proof data
                zkLoginSignature.Inputs = new Inputs
                {
                    ProofPoints = proofResponse.ProofPoints,
                    IssBase64Details = proofResponse.IssBase64Details,
                    HeaderBase64 = BigInteger.Parse(proofResponse.HeaderBase64),
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

                var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
                Debug.Log($"ProofPoints 2: {jsonObj}");

                // Create and manually populate the response
                var result = new ZkProofResponse
                {
                    IssBase64Details = new ZkLoginSignatureInputsClaim
                    {
                        Value = jsonObj["issBase64Details"]["value"].ToString(),
                        IndexMod4 = byte.Parse(jsonObj["issBase64Details"]["indexMod4"].ToString())
                    },
                    HeaderBase64 = jsonObj["headerBase64"].ToString(),  // Keep as string for now
                    DataToSign = Convert.FromBase64String(jsonObj["dataToSign"].ToString())
                };
                Debug.Log($"ProofPoints 1: {result}");

                // Create manually populated ProofPoints to avoid circular reference issues
                result.ProofPoints = new ProofPoints();

                // Create new instances for each sequence to avoid circular references
                result.ProofPoints.A = CreateSequenceFromArray(jsonObj["proofPoints"]["a"]);
                result.ProofPoints.B = CreateNestedSequence(jsonObj["proofPoints"]["b"]);
                result.ProofPoints.C = CreateSequenceFromArray(jsonObj["proofPoints"]["c"]);

                Debug.Log($"ProofPoints : {result}");

                return result;
            }

        }
        private static Sequence CreateSequenceFromArray(Newtonsoft.Json.Linq.JToken array)
        {
            var items = array.ToArray();
            var bStrings = items.Select(item => new OpenDive.BCS.BString(item.ToString())).ToArray();
            return new OpenDive.BCS.Sequence(bStrings);
        }

        private static Sequence CreateNestedSequence(Newtonsoft.Json.Linq.JToken nestedArray)
        {
            // For the B field which is an array of arrays
            var outerItems = nestedArray.ToArray();
            var innerSequences = new List<ISerializable>();

            foreach (var innerArray in outerItems)
            {
                var innerItems = innerArray.ToArray();
                var innerBStrings = innerItems.Select(item => new OpenDive.BCS.BString(item.ToString())).ToArray();
                innerSequences.Add(new OpenDive.BCS.Sequence(innerBStrings));
            }

            return new OpenDive.BCS.Sequence(innerSequences.ToArray());
        }
    }
}

[Serializable]
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ZkProofRequest
{
    [JsonProperty("jwt")]
    public string Jwt;

    //[JsonProperty("addressSeed")]
    //public string AddressSeed;

    [JsonProperty("salt")]
    public string Salt;

    [JsonProperty("keyClaimName")]
    public string KeyClaimName;

    [JsonProperty("maxEpoch")]
    public ulong MaxEpoch;

    [JsonProperty("jwtRandomness")]
    public string JwtRandomness;

    //[JsonProperty("ephemeralPublicKey")]
    //public string EphemeralPublicKey;

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
    public string HeaderBase64;

    [JsonProperty("dataToSign")]
    public byte[] DataToSign;
}
