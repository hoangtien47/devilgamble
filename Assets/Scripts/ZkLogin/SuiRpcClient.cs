//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace ZkLogin
//{
//    public class SuiRpcClient
//    {
//        private readonly string rpcUrl;
//        private readonly HttpClient httpClient;
//        private int requestId = 0;

//        public SuiRpcClient(string rpcUrl)
//        {
//            this.rpcUrl = rpcUrl;
//            httpClient = new HttpClient();
//        }

//        public async Task<ulong> GetCurrentEpochAsync()
//        {
//            var request = new
//            {
//                jsonrpc = "2.0",
//                id = GetNextRequestId(),
//                method = "suix_getLatestSuiSystemState",
//                @params = Array.Empty<object>()
//            };

//            var responseJson = await SendRpcRequestAsync(request);

//            // Parse the response using JObject
//            var responseObj = JObject.Parse(responseJson);

//            // Check if there's an error
//            if (responseObj["error"] != null)
//            {
//                throw new Exception($"RPC error: {responseObj["error"]["message"]}");
//            }

//            // Extract epoch from the result
//            var epoch = responseObj["result"]["epoch"].Value<ulong>();
//            return epoch;
//        }

//        public async Task<string> ExecuteTransactionAsync(string txBytes, string signature, string address)
//        {
//            var request = new
//            {
//                jsonrpc = "2.0",
//                id = GetNextRequestId(),
//                method = "sui_executeTransaction",
//                @params = new object[]
//                {
//                    txBytes,
//                    signature,
//                    new { zkLogin = address },
//                    "WaitForLocalExecution"
//                }
//            };

//            var responseJson = await SendRpcRequestAsync(request);

//            // Parse the response
//            var responseObj = JObject.Parse(responseJson);

//            // Check if there's an error
//            if (responseObj["error"] != null)
//            {
//                throw new Exception($"RPC error: {responseObj["error"]["message"]}");
//            }

//            // Extract digest from the result
//            var digest = responseObj["result"]["digest"].Value<string>();
//            return digest;
//        }

//        private async Task<string> SendRpcRequestAsync(object request)
//        {
//            try
//            {
//                string json = JsonConvert.SerializeObject(request);
//                Debug.Log($"Sending RPC request to {rpcUrl}: {json}");

//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await httpClient.PostAsync(rpcUrl, content);
//                if (!response.IsSuccessStatusCode)
//                {
//                    var error = await response.Content.ReadAsStringAsync();
//                    Debug.LogError($"RPC request failed with status {response.StatusCode}: {error}");
//                    throw new Exception($"RPC request failed: {error}");
//                }

//                var responseJson = await response.Content.ReadAsStringAsync();
//                Debug.Log($"RPC response: {responseJson}");

//                return responseJson;
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError($"Error in SendRpcRequestAsync: {ex.Message}");
//                throw;
//            }
//        }

//        private int GetNextRequestId()
//        {
//            return ++requestId;
//        }
//    }
//}