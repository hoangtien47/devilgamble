//using Newtonsoft.Json;
//using OpenDive.BCS;
//using Sui.Cryptography.Ed25519;
//using Sui.ZKLogin;
//using Sui.ZKLogin.SDK;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace ZkLogin
//{
//    public static class ZkProofGenerator
//    {
//        // Primary prover URL for Devnet
//        private static readonly string PRIMARY_PROVER_URL = "https://prover-dev.mystenlabs.com/v1";

//        // Fallback prover URL if the primary one fails
//        private static readonly string FALLBACK_PROVER_URL = "https://zklogin-prover-dev.mystenlabs.com/v1";

//        // Current prover URL to use
//        private static string PROVER_URL = PRIMARY_PROVER_URL;

//        public static async Task<ZkLoginSignature> GenerateZkProofAsync(
//            string jwt,
//            string userSalt,
//            string ephemeralPrivateKeyB64,
//            ulong maxEpoch,
//            string savedRandomness = null) // Add parameter for saved randomness
//        {
//            try
//            {

//                Debug.Log("Starting ZK proof generation process");

//                // Decode the JWT to get necessary fields
//                var decodedJwt = OpenDive.Utils.Jwt.JWTDecoder.DecodeJWT(jwt);
//                if (decodedJwt == null)
//                {
//                    throw new Exception("Failed to decode JWT");
//                }

//                // Create the ephemeral key pair
//                var ephemeralPrivateKey = new PrivateKey(ephemeralPrivateKeyB64);
//                var ephemeralPublicKeyBase = ephemeralPrivateKey.PublicKey();

//                // Create a proper Ed25519.PublicKey using the key data from the base class
//                var ephemeralPublicKey = new PublicKey(ephemeralPublicKeyBase.KeyBase64);
//                Debug.Log($"Zk Private key: {ephemeralPrivateKey}");
//                Debug.Log($"Zk Public key: {ephemeralPublicKey}");
//                // Use saved randomness if provided, otherwise generate new randomness
//                string randomness = savedRandomness;
//                if (string.IsNullOrEmpty(randomness))
//                {
//                    randomness = NonceGenerator.GenerateRandomness();
//                    Debug.Log("Generated new randomness for proof");
//                }
//                else
//                {
//                    Debug.Log("Using provided randomness from authentication");
//                }

//                // Generate nonce with the randomness
//                string nonce = NonceGenerator.GenerateNonce(
//                    ephemeralPublicKey,
//                    (int)maxEpoch,
//                    randomness
//                );
//                Debug.Log($"ZK Generated nonce for proof: {nonce}");

//                // Calculate the address seed
//                BigInteger addressSeed;
//                try
//                {
//                    addressSeed = Utils.GenAddressSeed(
//                        userSalt,
//                        "sub", // Using "sub" as the key claim
//                        decodedJwt.Payload.Sub,
//                        decodedJwt.Payload.Aud
//                    );
//                    Debug.Log($"Successfully generated address seed: {addressSeed}");
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Error generating address seed: {ex.Message}");
//                    // Use a fallback value if the address seed generation fails
//                    addressSeed = BigInteger.Parse("123456789012345678901234567890");
//                    Debug.LogWarning($"Using fallback address seed: {addressSeed}");
//                }

//                // Create a proof request to send to the prover service
//                var proofRequest = new ZkProofRequest
//                {
//                    Jwt = jwt,
//                    //AddressSeed = addressSeed.ToString(),
//                    Salt = userSalt,
//                    KeyClaimName = "sub", // Add the key claim name (typically "sub" for subject)
//                    MaxEpoch = maxEpoch,
//                    JwtRandomness = randomness, // Use the consistent randomness
//                    //EphemeralPublicKey = ephemeralPublicKeyBase.KeyBase64,
//                    ExtendedEphemeralPublicKey = ephemeralPublicKeyBase.KeyBase64
//                };

//                // Set up JSON serialization settings if needed
//                var jsonSettings = new JsonSerializerSettings
//                {
//                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
//                    Formatting = Formatting.Indented
//                };
//                // Call the prover service to get the proof
//                ZkProofResponse proofResponse;
//                try
//                {
//                    proofResponse = await SendProofRequestAsync(proofRequest);
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Error from prover service: {ex.Message}");

//                    // Create a mock response for testing purposes
//                    if (Application.isEditor)
//                    {
//                        Debug.LogWarning("Creating mock proof response for testing in Editor mode");
//                        proofResponse = CreateMockProofResponse();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }

//                // Validate the proof response
//                if (proofResponse == null)
//                {
//                    throw new Exception("Received null proof response from prover service");
//                }

//                if (proofResponse.ProofPoints == null)
//                {
//                    Debug.LogWarning("ProofPoints is null in the response");
//                }

//                if (proofResponse.IssBase64Details == null)
//                {
//                    Debug.LogWarning("IssBase64Details is null in the response");
//                }

//                // Create the ZK Login signature components
//                var zkLoginSignature = new ZkLoginSignature();

//                // Populate the ZkLoginSignature with the proof data
//                try
//                {
//                    // Convert the Base64 header to a numeric value
//                    BigInteger headerBase64Value;
//                    if (!string.IsNullOrEmpty(proofResponse.HeaderBase64))
//                    {
//                        // Convert Base64 to bytes and then to BigInteger
//                        byte[] headerBytes = Encoding.UTF8.GetBytes(proofResponse.HeaderBase64);

//                        // Create a numeric hash of the header bytes
//                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
//                        {
//                            byte[] hashBytes = sha256.ComputeHash(headerBytes);
//                            // Convert only the first 8 bytes to a BigInteger (to avoid overflow)
//                            byte[] truncatedBytes = new byte[8];
//                            Array.Copy(hashBytes, truncatedBytes, 8);
//                            headerBase64Value = new BigInteger(truncatedBytes);
//                        }
//                    }
//                    else
//                    {
//                        headerBase64Value = BigInteger.Zero;
//                        Debug.LogWarning("HeaderBase64 is null or empty, using zero");
//                    }

//                    zkLoginSignature.Inputs = new Inputs
//                    {
//                        ProofPoints = proofResponse.ProofPoints ?? new ProofPoints(),
//                        IssBase64Details = proofResponse.IssBase64Details ?? new ZkLoginSignatureInputsClaim { Value = "", IndexMod4 = 0 },
//                        HeaderBase64 = headerBase64Value,
//                        AddressSeed = addressSeed.ToString()
//                    };
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Error creating ZkLoginSignature inputs: {ex.Message}");
//                    throw new Exception($"Failed to create ZkLoginSignature inputs: {ex.Message}", ex);
//                }

//                zkLoginSignature.MaxEpoch = maxEpoch;

//                // Sign with the ephemeral private key and convert signature to byte array
//                try
//                {
//                    if (proofResponse.DataToSign != null && proofResponse.DataToSign.Length > 0)
//                    {
//                        var signature = ephemeralPrivateKey.Sign(proofResponse.DataToSign);
//                        zkLoginSignature.UserSignature = signature.SignatureBytes;
//                    }
//                    else
//                    {
//                        // If there's no data to sign, create a signature for a default message
//                        byte[] defaultMessage = Encoding.UTF8.GetBytes("default_message_for_signature");
//                        var signature = ephemeralPrivateKey.Sign(defaultMessage);
//                        zkLoginSignature.UserSignature = signature.SignatureBytes;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Error generating signature: {ex.Message}");
//                    // Create an empty signature as a last resort
//                    zkLoginSignature.UserSignature = new byte[64]; // Standard Ed25519 signature length
//                }

//                return zkLoginSignature;
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError($"Error generating ZK proof: {ex.Message}");
//                throw;
//            }
//        }

//        private static async Task<ZkProofResponse> SendProofRequestAsync(ZkProofRequest request)
//        {
//            // Set up the JSON serialization settings
//            var jsonSettings = new JsonSerializerSettings
//            {
//                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
//                NullValueHandling = NullValueHandling.Ignore,
//                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
//                Formatting = Formatting.Indented
//            };

//            // Serialize the request
//            string jsonRequest = JsonConvert.SerializeObject(request, jsonSettings);
//            Debug.Log($"Sending request to prover: {jsonRequest}");

//            // Create the content for the HTTP request
//            var content = new System.Net.Http.StringContent(
//                jsonRequest,
//                Encoding.UTF8,
//                "application/json"
//            );

//            // Maximum number of retry attempts
//            const int maxRetries = 3;
//            // Initial delay between retries in milliseconds
//            const int initialRetryDelayMs = 1000;

//            // Try both prover URLs with retries
//            for (int urlAttempt = 0; urlAttempt < 2; urlAttempt++)
//            {
//                string currentProverUrl = urlAttempt == 0 ? PRIMARY_PROVER_URL : FALLBACK_PROVER_URL;

//                // Try with retries for each URL
//                for (int retryCount = 0; retryCount < maxRetries; retryCount++)
//                {
//                    try
//                    {
//                        // Create a new HTTP client for each attempt with a timeout
//                        using (var httpClient = new System.Net.Http.HttpClient())
//                        {
//                            // Set a reasonable timeout (10 seconds)
//                            httpClient.Timeout = TimeSpan.FromSeconds(10);

//                            // Add recommended headers
//                            httpClient.DefaultRequestHeaders.Accept.Add(
//                                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
//                            );

//                            // Call the prover service
//                            var response = await httpClient.PostAsync(currentProverUrl, content);
//                            string responseContent = await response.Content.ReadAsStringAsync();

//                            if (response.IsSuccessStatusCode)
//                            {
//                                // If successful, update the current prover URL for future calls
//                                PROVER_URL = currentProverUrl;

//                                // Process the response and return
//                                return ProcessProverResponse(responseContent);
//                            }
//                            else
//                            {
//                                // If we've exhausted retries for this URL, we'll try the next URL
//                                if (retryCount == maxRetries - 1)
//                                {
//                                    Debug.LogError($"Failed to get response from {currentProverUrl} after {maxRetries} attempts");
//                                    break;
//                                }
//                            }
//                        }
//                    }
//                    catch (System.Net.Http.HttpRequestException ex)
//                    {
//                        Debug.LogWarning($"HTTP request error with {currentProverUrl}: {ex.Message}");
//                    }
//                    catch (TaskCanceledException ex)
//                    {
//                        Debug.LogWarning($"Request timeout with {currentProverUrl}: {ex.Message}");
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.LogWarning($"Unexpected error with {currentProverUrl}: {ex.Message}");
//                    }

//                    // Wait before retrying with exponential backoff
//                    int delayMs = initialRetryDelayMs * (int)Math.Pow(2, retryCount);
//                    await Task.Delay(delayMs);
//                }
//            }

//            // If we've tried all URLs and all retries, throw an exception
//            throw new Exception("Failed to connect to any prover service after multiple attempts");
//        }

//        private static ZkProofResponse ProcessProverResponse(string responseContent)
//        {
//            var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(responseContent);

//            // Create and manually populate the response
//            var result = new ZkProofResponse();

//            // Handle issBase64Details
//            if (jsonObj["issBase64Details"] != null)
//            {
//                result.IssBase64Details = new ZkLoginSignatureInputsClaim
//                {
//                    Value = jsonObj["issBase64Details"]["value"]?.ToString() ?? "",
//                    IndexMod4 = jsonObj["issBase64Details"]["indexMod4"] != null ?
//                        byte.Parse(jsonObj["issBase64Details"]["indexMod4"].ToString()) : (byte)0
//                };
//            }
//            else
//            {
//                result.IssBase64Details = new ZkLoginSignatureInputsClaim { Value = "", IndexMod4 = 0 };
//            }

//            // Handle headerBase64
//            result.HeaderBase64 = jsonObj["headerBase64"]?.ToString() ?? "";

//            // Handle dataToSign field which might be missing in some responses
//            if (jsonObj["dataToSign"] != null)
//            {
//                try
//                {
//                    result.DataToSign = Convert.FromBase64String(jsonObj["dataToSign"].ToString());
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogWarning($"Error converting dataToSign to bytes: {ex.Message}");
//                    result.DataToSign = new byte[0];
//                }
//            }
//            else
//            {
//                // Create an empty byte array if dataToSign is missing
//                result.DataToSign = new byte[0];
//            }

//            // Create manually populated ProofPoints to avoid circular reference issues
//            result.ProofPoints = new ProofPoints();

//            // Create new instances for each sequence to avoid circular references
//            if (jsonObj["proofPoints"] != null)
//            {
//                try
//                {
//                    if (jsonObj["proofPoints"]["a"] != null)
//                    {
//                        result.ProofPoints.A = CreateSequenceFromArray(jsonObj["proofPoints"]["a"]);
//                    }

//                    if (jsonObj["proofPoints"]["b"] != null)
//                    {
//                        result.ProofPoints.B = CreateNestedSequence(jsonObj["proofPoints"]["b"]);
//                    }

//                    if (jsonObj["proofPoints"]["c"] != null)
//                    {
//                        result.ProofPoints.C = CreateSequenceFromArray(jsonObj["proofPoints"]["c"]);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Error creating proof points: {ex.Message}");
//                    // Create empty sequences if there's an error
//                    result.ProofPoints.A = new OpenDive.BCS.Sequence(new OpenDive.BCS.BString[0]);
//                    result.ProofPoints.B = new OpenDive.BCS.Sequence(new OpenDive.BCS.Sequence[0]);
//                    result.ProofPoints.C = new OpenDive.BCS.Sequence(new OpenDive.BCS.BString[0]);
//                }
//            }
//            else
//            {
//                // Create empty sequences
//                result.ProofPoints.A = new OpenDive.BCS.Sequence(new OpenDive.BCS.BString[0]);
//                result.ProofPoints.B = new OpenDive.BCS.Sequence(new OpenDive.BCS.Sequence[0]);
//                result.ProofPoints.C = new OpenDive.BCS.Sequence(new OpenDive.BCS.BString[0]);
//            }

//            return result;
//        }

//        private static Sequence CreateSequenceFromArray(Newtonsoft.Json.Linq.JToken array)
//        {
//            var items = array.ToArray();
//            var bStrings = items.Select(item => new OpenDive.BCS.BString(item.ToString())).ToArray();
//            return new OpenDive.BCS.Sequence(bStrings);
//        }

//        private static Sequence CreateNestedSequence(Newtonsoft.Json.Linq.JToken nestedArray)
//        {
//            // For the B field which is an array of arrays
//            var outerItems = nestedArray.ToArray();
//            var innerSequences = new List<ISerializable>();

//            foreach (var innerArray in outerItems)
//            {
//                var innerItems = innerArray.ToArray();
//                var innerBStrings = innerItems.Select(item => new OpenDive.BCS.BString(item.ToString())).ToArray();
//                innerSequences.Add(new OpenDive.BCS.Sequence(innerBStrings));
//            }

//            return new OpenDive.BCS.Sequence(innerSequences.ToArray());
//        }

//        /// <summary>
//        /// Creates a mock proof response for testing purposes when the prover service is unavailable
//        /// </summary>
//        private static ZkProofResponse CreateMockProofResponse()
//        {
//            Debug.LogWarning("Using mock ZK proof response for testing - NOT FOR PRODUCTION USE");

//            var response = new ZkProofResponse
//            {
//                IssBase64Details = new ZkLoginSignatureInputsClaim
//                {
//                    Value = "yJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLC",
//                    IndexMod4 = 1
//                },
//                HeaderBase64 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImUxNGMzN2Q2ZTVjNzU2ZThiNzJmZGI1MDA0YzBjYzM1NjMzNzkyNGUiLCJ0eXAiOiJKV1QifQ",
//                DataToSign = new byte[0],
//                ProofPoints = new ProofPoints()
//            };

//            // Create mock proof points with placeholder values
//            var mockValueA = new[] { "1", "2", "3" };
//            var mockValueB = new[] { new[] { "4", "5" }, new[] { "6", "7" }, new[] { "1", "0" } };
//            var mockValueC = new[] { "8", "9", "10" };

//            // Create sequences for A and C
//            response.ProofPoints.A = new OpenDive.BCS.Sequence(
//                mockValueA.Select(v => new OpenDive.BCS.BString(v)).ToArray()
//            );

//            // Create nested sequence for B
//            var sequencesB = mockValueB.Select(subArray =>
//                new OpenDive.BCS.Sequence(
//                    subArray.Select(v => new OpenDive.BCS.BString(v)).ToArray()
//                )
//            ).ToArray();
//            response.ProofPoints.B = new OpenDive.BCS.Sequence(sequencesB);

//            // Create sequence for C
//            response.ProofPoints.C = new OpenDive.BCS.Sequence(
//                mockValueC.Select(v => new OpenDive.BCS.BString(v)).ToArray()
//            );

//            return response;
//        }
//    }
//}

//[Serializable]
//[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
//public class ZkProofRequest
//{
//    [JsonProperty("jwt")]
//    public string Jwt;

//    //[JsonProperty("addressSeed")]
//    //public string AddressSeed;

//    [JsonProperty("salt")]
//    public string Salt;

//    [JsonProperty("keyClaimName")]
//    public string KeyClaimName;

//    [JsonProperty("maxEpoch")]
//    public ulong MaxEpoch;

//    [JsonProperty("jwtRandomness")]
//    public string JwtRandomness;

//    //[JsonProperty("ephemeralPublicKey")]
//    //public string EphemeralPublicKey;

//    [JsonProperty("extendedEphemeralPublicKey")]
//    public string ExtendedEphemeralPublicKey;
//}

//[Serializable]
//public class ZkProofResponse
//{
//    [JsonProperty("proofPoints")]
//    public ProofPoints ProofPoints;

//    [JsonProperty("issBase64Details")]
//    public ZkLoginSignatureInputsClaim IssBase64Details;

//    [JsonProperty("headerBase64")]
//    public string HeaderBase64;

//    [JsonProperty("dataToSign")]
//    public byte[] DataToSign;
//}
