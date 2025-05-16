using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ZkLogin
{
    public class CustomJWTDecoder
    {
        [Serializable]
        public class JWTHeader
        {
            public string alg;
            public string typ;
            public string kid;
        }

        [Serializable]
        public class JWTPayload
        {
            public string iss;
            public string sub;
            public string name;
            public string picture;
            public string email;
            public JToken aud; // Using JToken to handle both string and array formats
            public long exp;
            public long iat;
            public string nonce;
        }

        public static string NormalizeJwt(string originalJwt)
        {
            try
            {
                // Split the JWT into its components
                string[] parts = originalJwt.Split('.');
                if (parts.Length != 3)
                {
                    Debug.LogError("Invalid JWT format");
                    return originalJwt;
                }

                // Decode the payload
                string payloadJson = Decode(parts[1]);
                Debug.Log($"Original payload: {payloadJson}");

                // Parse to JObject to manipulate
                JObject payloadObj = JObject.Parse(payloadJson);

                // Check if 'aud' is an array
                if (payloadObj["aud"] != null && payloadObj["aud"].Type == JTokenType.Array)
                {
                    // Convert to string by taking the first value
                    if (payloadObj["aud"].Count() > 0)
                    {
                        string firstAud = payloadObj["aud"][0].Value<string>();
                        payloadObj["aud"] = firstAud;
                        Debug.Log($"Converting 'aud' array to string: {firstAud}");
                    }
                }

                // Re-encode the payload
                string modifiedPayload = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(payloadObj.ToString(Formatting.None)))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');

                // Build the new JWT
                string normalizedJwt = $"{parts[0]}.{modifiedPayload}.{parts[2]}";
                Debug.Log("Successfully normalized JWT for ZKLogin");

                return normalizedJwt;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error normalizing JWT: {e.Message}");
                return originalJwt; // Return original on error
            }
        }

        public static JWTPayload DecodePayload(string jwt)
        {
            try
            {
                string[] parts = jwt.Split('.');
                if (parts.Length != 3)
                {
                    Debug.LogError("Invalid JWT format");
                    return null;
                }

                string payloadJson = Decode(parts[1]);
                return JsonConvert.DeserializeObject<JWTPayload>(payloadJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error decoding JWT payload: {e.Message}");
                return null;
            }
        }

        private static string Decode(string encoded)
        {
            // Add padding if needed
            string padding = string.Empty;
            if (encoded.Length % 4 == 2) padding = "==";
            else if (encoded.Length % 4 == 3) padding = "=";

            // Fix URL encoding
            string fixed_encoded = encoded
                .Replace('-', '+')
                .Replace('_', '/') + padding;

            byte[] bytes = Convert.FromBase64String(fixed_encoded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}