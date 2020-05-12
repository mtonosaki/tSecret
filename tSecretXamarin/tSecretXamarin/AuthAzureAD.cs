// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tono;
using tSecretCommon;

namespace tSecretXamarin
{
    public class AuthAzureAD
    {
        public string DisplayName { get; protected set; }
        public bool IsAuthenticated { get; protected set; }
        public string TenantID => authResult?.TenantId;
        public string UserObjectID => authResult?.UniqueId;
        public Func<object> UIParent { get; set; }
        public Func<object> MainActivity { get; set; }

        private readonly string[] scopes = new[] { "user.read" };
        private readonly MySecretParameter param = new MySecretParameterXamarin();
        private IPublicClientApplication clientApp = null;
        private AuthenticationResult authResult = null;

        public void Initialize()
        {
            if (clientApp == null)
            {
                IsAuthenticated = false;

                clientApp = PublicClientApplicationBuilder.Create(param.AzureADClientId)
                    .WithAuthority(param.AuthorityAudience)
                    .WithRedirectUri(param.PublicClientRedirectUri)
                    .WithIosKeychainSecurityGroup(param.IosKeychainSecurityGroups)
                    .WithParentActivityOrWindow(() => MainActivity?.Invoke() ?? UIParent?.Invoke())
                    .Build();
            }
        }


        public async Task<bool> LoginSilentAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            Initialize();
            var ret = false;
            try
            {
                var accounts = await clientApp.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();
                authResult = await clientApp
                                .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                .ExecuteAsync(node.CTS.Token);   // Login automatically

                IsAuthenticated = true;
                ret = true;
            }
            catch (MsalUiRequiredException)
            {
                authResult = null;
            }
            catch (Exception ex)
            {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Silently:");
                node.MessageBuffer?.WriteLine(ex.Message);
                node.MessageBuffer?.WriteLine("(E101)");
            }
            return ret;
        }

        public async Task<bool> LoginInteractiveAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            Initialize();
            var ret = false;
            try
            {
                authResult = await clientApp
                    .AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithParentActivityOrWindow(UIParent?.Invoke())
                    .ExecuteAsync();

                IsAuthenticated = true;
                ret = true;
            }
            catch (MsalException msalex)
            {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Interactive");
                node.MessageBuffer?.WriteLine(msalex.Message);
                node.MessageBuffer?.WriteLine("(E102)");
            }
            catch (Exception ex)
            {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Interactive");
                node.MessageBuffer?.WriteLine(ex.Message);
                node.MessageBuffer?.WriteLine("(E101)");
            }
            return ret;
        }
        public async Task<bool> LogoutAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            var ret = false;

            if (IsAuthenticated)
            {
                try
                {
                    var accounts = await clientApp.GetAccountsAsync();
                    while (accounts.Any())
                    {
                        await clientApp.RemoveAsync(accounts.First());
                        accounts = await clientApp.GetAccountsAsync();
                    }
                    IsAuthenticated = false;
                }
                catch (MsalException ex)
                {
                    node.MessageBuffer?.WriteLine($"Signing-out user:");
                    node.MessageBuffer?.WriteLine(ex.Message);
                    node.MessageBuffer?.WriteLine("(E114)");

                }
            }
            return ret;
        }


        public async Task<bool> GetPrivacyDataAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            if (IsAuthenticated && authResult != null)
            {
                var content = await GetHttpContentWithTokenAsync(param.GraphAPIEndpoint, authResult.AccessToken, node.CTS.Token, node.MessageBuffer)
                                    .ConfigureAwait(false);
                if (string.IsNullOrEmpty(content))
                {
                    return false;
                }
                var s1 = StrUtil.MidSkip(content ?? "", "\"displayName\"\\s*:\\s*\"");
                var dn = StrUtil.LeftBefore(s1 + "\"", "\"").Trim();           // pick displayName part
                if (string.IsNullOrEmpty(dn) == false)
                {
                    DisplayName = dn;
                }
                return true;
            }
            else
            {
                node.MessageBuffer?.WriteLine($"Warning : Could not load privacy data from Microsoft Graph");
                node.MessageBuffer?.WriteLine("(E115)");
                return false;
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="authToken">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        private async Task<string> GetHttpContentWithTokenAsync(string url, string authToken, CancellationToken token, StreamWriter log)
        {
            var httpClient = new System.Net.Http.HttpClient();  // TODO: to be static object
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JsonConvert.DeserializeObject(content);  // to decode escape sequences of JSON
                return json.ToString();
            }
            catch (Exception ex)
            {
                log?.WriteLine($"Signing user:");
                log?.WriteLine(ex.Message);
                log?.WriteLine("(E114)");
                return "";
            }
        }
    }
}
