#if UNITY_EDITOR
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ZkLogin
{
    public class EditorOAuthServer : IDisposable
    {
        private HttpListener listener;
        private CancellationTokenSource cancellationSource;
        private TaskCompletionSource<string> authCompletionSource;
        private int port;

        public EditorOAuthServer(int port = 8080)
        {
            this.port = port;
        }

        public Task<string> StartListeningAsync()
        {
            authCompletionSource = new TaskCompletionSource<string>();
            cancellationSource = new CancellationTokenSource();

            try
            {
                // Start the HTTP listener
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/oauth2redirect/");
                listener.Start();
                Debug.Log($"OAuth server listening on port {port}");

                // Begin listening for requests
                ListenForCallback();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start OAuth server: {ex.Message}");
                authCompletionSource.TrySetException(ex);
            }

            return authCompletionSource.Task;
        }

        private async void ListenForCallback()
        {
            try
            {
                // Wait for the callback request
                HttpListenerContext context = await listener.GetContextAsync();

                // Get the full URL of the request
                string fullUrl = context.Request.Url.ToString();
                Debug.Log($"Received OAuth callback: {fullUrl}");

                // The token extraction page
                string responseHtml = @"
                <html>
                <head>
                    <title>Authentication Complete</title>
                    <script>
                        function sendTokenToUnity() {
                            // Get token from fragment
                            var fragment = window.location.hash.substring(1);
                            var params = new URLSearchParams(fragment);
                            var token = params.get('id_token');
                            
                            if (token) {
                                // Display success
                                document.getElementById('status').innerHTML = 
                                    '<span style=""color: green"">Authentication successful!</span><br>' +
                                    'You can close this window.';
                                
                                // Create special link that will be intercepted by the server
                                var tokenLink = document.createElement('a');
                                tokenLink.href = '/oauth2redirect/capture-token?token=' + encodeURIComponent(token);
                                tokenLink.id = 'tokenLink';
                                tokenLink.style.display = 'none';
                                document.body.appendChild(tokenLink);
                                
                                // Click the link automatically
                                setTimeout(function() {
                                    document.getElementById('tokenLink').click();
                                }, 500);
                            } else {
                                document.getElementById('status').innerHTML = 
                                    '<span style=""color: red"">Error: No token found</span>';
                            }
                        }
                    </script>
                </head>
                <body onload='sendTokenToUnity()'>
                    <h2>Authentication Complete</h2>
                    <p id='status'>Processing authentication...</p>
                </body>
                </html>";

                // Send the extraction page
                byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();

                // Wait for the token capture request
                context = await listener.GetContextAsync();

                // Check if this is our token capture endpoint
                if (context.Request.Url.AbsolutePath.Contains("/capture-token"))
                {
                    string token = context.Request.QueryString["token"];

                    if (!string.IsNullOrEmpty(token))
                    {
                        // Send a simple success response
                        SendSuccessResponse(context);

                        // Complete the task with the token
                        authCompletionSource.TrySetResult(token);
                    }
                    else
                    {
                        SendErrorResponse(context, "No token provided");
                        authCompletionSource.TrySetException(new Exception("No token received"));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationSource.IsCancellationRequested)
                {
                    Debug.LogError($"Error in OAuth server: {ex.Message}");
                    authCompletionSource.TrySetException(ex);
                }
            }
            finally
            {
                // Dispose the server regardless
                Dispose();
            }
        }

        private void SendSuccessResponse(HttpListenerContext context)
        {
            string successHtml = @"
            <html>
            <head>
                <title>Token Received</title>
                <style>
                    body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }
                    .success { color: green; font-size: 20px; }
                </style>
            </head>
            <body>
                <h2>Authentication Successful</h2>
                <p class='success'>Token received! You can close this window.</p>
                <script>window.close();</script>
            </body>
            </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(successHtml);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private void SendErrorResponse(HttpListenerContext context, string error)
        {
            string errorHtml = $@"
            <html>
            <head>
                <title>Authentication Error</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                    .error {{ color: red; font-size: 18px; }}
                </style>
            </head>
            <body>
                <h2>Authentication Failed</h2>
                <p class='error'>{error}</p>
            </body>
            </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        public void Dispose()
        {
            cancellationSource?.Cancel();

            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener = null;
                Debug.Log("OAuth server stopped");
            }
        }
    }
}
#endif