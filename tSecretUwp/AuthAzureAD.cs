// (c) 2019 Manabu Tonosaki
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

namespace tSecretUwp {
    public class AuthAzureAD {
        public bool IsAuthenticated { get; set; }
        public string DisplayName { get; set; }

        private static readonly System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
        private readonly string[] scopes = new[] { "user.read" };
        private readonly MySecretParameter param = new MySecretParameter();
        private IPublicClientApplication clientApp = null;
        private AuthenticationResult authResult = null;

        public void Initialize() {
            if (clientApp == null) {
                IsAuthenticated = false;
                clientApp = PublicClientApplicationBuilder.Create(param.AzureADClientId)
                        .WithAuthority(param.AuthorityAudience)
                        .WithUseCorporateNetwork(true)
                        .WithRedirectUri(param.PublicClientRedirectUri)
                        .Build();
            }
        }

        public async Task<bool> LoginSilentAsync(Func<StoryNode> lazynode) {
            StoryNode node = lazynode();
            Initialize();
            bool ret = false;
            try {
                System.Collections.Generic.IEnumerable<IAccount> accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
                IAccount firstAccount = accounts.FirstOrDefault();
                authResult = await clientApp.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync(node.CTS.Token).ConfigureAwait(false); // Login automatically
                IsAuthenticated = true;

                ret = true;
            } catch (MsalUiRequiredException) {
                authResult = null;
            } catch (Exception ex) {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Silently:");
                node.MessageBuffer?.WriteLine(ex.Message);
                node.MessageBuffer?.WriteLine("(E101)");
            }
            return ret;
        }

        public async Task<bool> LoginInteractiveAsync(Func<StoryNode> lazynode) {
            StoryNode node = lazynode();
            Initialize();
            bool ret = false;
            try {
                authResult = await clientApp.AcquireTokenInteractive(scopes)
                                            .ExecuteAsync(node.CTS.Token)
                                            .ConfigureAwait(false);
                IsAuthenticated = true;
                ret = true;
            } catch (MsalException msalex) {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Interactive:");
                node.MessageBuffer?.WriteLine(msalex.Message);
                node.MessageBuffer?.WriteLine("(E102)");
            } catch (Exception ex) {
                authResult = null;
                node.MessageBuffer?.WriteLine($"Acquiring Token Interactive:");
                node.MessageBuffer?.WriteLine(ex.Message);
                node.MessageBuffer?.WriteLine("(E101)");
            }
            return ret;
        }
        public async Task<bool> LogoutAsync(Func<StoryNode> lazynode) {
            StoryNode node = lazynode();
            bool ret = false;

            if (IsAuthenticated) {
                try {
                    System.Collections.Generic.IEnumerable<IAccount> accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
                    IAccount firstAccount = accounts.FirstOrDefault();
                    await clientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
                    IsAuthenticated = false;
                } catch (MsalException ex) {
                    node.MessageBuffer?.WriteLine($"Signing-out user:");
                    node.MessageBuffer?.WriteLine(ex.Message);
                    node.MessageBuffer?.WriteLine("(E114)");

                }
            }
            return ret;
        }

        public async Task<bool> GetPrivacyDataAsync(Func<StoryNode> lazynode) {
            StoryNode node = lazynode();
            if (IsAuthenticated && authResult != null) {
                MySecretParameter param = new MySecretParameter();
                string content = await GetHttpContentWithTokenAsync(param.GraphAPIEndpoint, authResult.AccessToken, node.CTS.Token, node.MessageBuffer)
                                    .ConfigureAwait(false);
                if (string.IsNullOrEmpty(content)) {
                    return false;
                }
                string s1 = StrUtil.MidSkip(content ?? "", "\"displayName\"\\s*:\\s*\"");
                string dn = StrUtil.LeftBefore(s1 + "\"", "\"").Trim();           // pick displayName part
                if (string.IsNullOrEmpty(dn) == false) {
                    DisplayName = dn;
                }
                return true;
            } else {
                node.MessageBuffer?.WriteLine($"Warning : Could not load privacy data from Microsoft Graph");
                node.MessageBuffer?.WriteLine("(E115)");
                return false;
            }
        }

        public string TenantID => authResult?.TenantId;

        public string UserObjectID => authResult?.UniqueId;

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="authToken">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        private async Task<string> GetHttpContentWithTokenAsync(string url, string authToken, CancellationToken token, StreamWriter log) {
            try {
                System.Net.Http.HttpRequestMessage request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                System.Net.Http.HttpResponseMessage response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                object json = JsonConvert.DeserializeObject(content);  // to decode escape sequences of JSON
                return json.ToString();
            } catch (Exception ex) {
                log?.WriteLine($"Signing-out user:");
                log?.WriteLine(ex.Message);
                log?.WriteLine("(E114)");
                return "";
            }
        }
    }
}
