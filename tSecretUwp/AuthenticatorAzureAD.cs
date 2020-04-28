using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tono;
using tSecretCommon;

namespace tSecretUwp
{
    public class AuthenticatorAzureAD : Authenticator
    {
        public string ErrorMessage { get; private set; }

        private IPublicClientApplication clientApp;
        private AuthenticationResult authResult = null;

        public override async Task<bool> LoginAsync()
        {
            var param = new MySecretParameter();
            clientApp = PublicClientApplicationBuilder.Create(param.AzureADClientId)
                    .WithAuthority(param.AuthorityAudience)
                    .WithUseCorporateNetwork(true)
                    .WithRedirectUri(param.PublicClientRedirectUri)
                    .Build();

            var accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
            var firstAccount = accounts.FirstOrDefault();
            var scopes = new string[] { "user.read" };
            IsSilentLogin = false;

            try
            {
                authResult = await clientApp.AcquireTokenSilent(scopes, firstAccount)     // Login automatically
                                                  .ExecuteAsync();
                IsSilentLogin = true;
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    authResult = await clientApp.AcquireTokenInteractive(scopes)          // Login with user authentication
                                                      .ExecuteAsync()
                                                      .ConfigureAwait(false);
                }
                catch (MsalException msalex)
                {
                    ErrorMessage = $"Error Acquiring Token:{System.Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}";
                return false;
            }

            IsAuthenticated = true;
            _displayName = authResult.UniqueId;
            return IsAuthenticated;
        }

        public override async Task LogoutAsync()
        {
            if (IsAuthenticated)
            {
                var accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
                var firstAccount = accounts.FirstOrDefault();

                try
                {
                    await clientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
                    IsAuthenticated = false;
                }
                catch (MsalException ex)
                {
                    ErrorMessage = $"Error signing-out user: {ex.Message}";
                }
            }
        }

        public override async Task<string> GetPrivacyData()
        {
            if (authResult != null)
            {
                var param = new MySecretParameter();
                var content = await GetHttpContentWithTokenAsync(param.GraphAPIEndpoint, authResult.AccessToken).ConfigureAwait(false);
                return content;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantID => authResult?.TenantId;
        public override string UserObjectID => authResult?.UniqueId;
        public override string DisplayName => _displayName;

        private string _displayName = "";

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        private async Task<string> GetHttpContentWithTokenAsync(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                var json = JsonConvert.DeserializeObject(content);
                content = json.ToString();

                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
