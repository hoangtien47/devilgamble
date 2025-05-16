using System;
using System.Threading.Tasks;

namespace Assets.SimpleFacebookSignIn.Scripts
{
    public partial class FacebookAuth
    {
        /// <summary>
        /// Returns an access token async.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            var completed = false;
            string accessToken = null, error = null;

            GetAccessToken((success, e, tokenResponse) =>
            {
                if (success)
                {
                    accessToken = tokenResponse?.AccessToken;
                }
                else
                {
                    error = e;
                }

                completed = true;
            });

            while (!completed)
            {
                await Task.Yield();
            }

            if (accessToken == null) throw new Exception(error);

            Log($"accessToken={accessToken}");

            return accessToken;
        }
    }
}