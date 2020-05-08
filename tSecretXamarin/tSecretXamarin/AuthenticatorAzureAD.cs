﻿// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

#if false
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
    public class AuthenticatorAzureAD : Authenticator
    {
        public object UIParent { get; set; } = null;
        private readonly string[] scopes = new[] { "user.read" };
        private readonly MySecretParameter param = new MySecretParameter();
        private IPublicClientApplication clientApp = null;
        private AuthenticationResult authResult = null;

        public void Initialize()
        {
            if (clientApp == null)
            {
                IsAuthenticated = false;

                clientApp = PublicClientApplicationBuilder.Create(param.AzureADClientId)
                    .WithIosKeychainSecurityGroup(param.IosKeychainSecurityGroups)
                    .WithB2CAuthority(param.AuthorityAudience)  // TODO:
                    .WithRedirectUri($"msal{param.AzureADClientId}://auth")
                    .Build();
            }
        }

        public override async Task<bool> LoginSilentAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            Initialize();
            var ret = false;
            try
            {
                var accounts = await clientApp.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();
                authResult = await clientApp.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync(node.CTS.Token); // Login automatically
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
                node.Message?.WriteLine($"Acquiring Token Silently:");
                node.Message?.WriteLine(ex.Message);
                node.Message?.WriteLine("(E101)");
            }
            return ret;
        }

        public override async Task<bool> LoginInteractiveAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            Initialize();
            var ret = false;
            try
            {
                authResult = await clientApp
                                            .AcquireTokenInteractive(scopes)
                                            .WithPrompt(Prompt.SelectAccount)
                                            .WithParentActivityOrWindow(UIParent)
                                            .ExecuteAsync();

                IsAuthenticated = true;
                ret = true;
            }
            catch (MsalException msalex)
            {
                authResult = null;
                node.Message?.WriteLine($"Acquiring Token Interactive:");
                node.Message?.WriteLine(msalex.Message);
                node.Message?.WriteLine("(E102)");
            }
            catch (Exception ex)
            {
                authResult = null;
                node.Message?.WriteLine($"Acquiring Token Interactive:");
                node.Message?.WriteLine(ex.Message);
                node.Message?.WriteLine("(E101)");
            }
            return ret;
        }
        public override async Task<bool> LogoutAsync(Func<StoryNode> lazynode)
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
                    node.Message?.WriteLine($"Signing-out user:");
                    node.Message?.WriteLine(ex.Message);
                    node.Message?.WriteLine("(E114)");

                }
            }
            return ret;
        }

        public override async Task<bool> GetPrivacyDataAsync(Func<StoryNode> lazynode)
        {
            var node = lazynode();
            if (IsAuthenticated && authResult != null)
            {
                var param = new MySecretParameter();
                var content = await GetHttpContentWithTokenAsync(param.GraphAPIEndpoint, authResult.AccessToken, node.CTS.Token, node.Message)
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
                node.Message?.WriteLine($"Warning : Could not load privacy data from Microsoft Graph");
                node.Message?.WriteLine("(E115)");
                return false;
            }
        }

        public string TenantID => authResult?.TenantId;

        public override string UserObjectID
        {
            get => authResult?.UniqueId;
            protected set => base.UserObjectID = value;
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
                log?.WriteLine($"Signing-out user:");
                log?.WriteLine(ex.Message);
                log?.WriteLine("(E114)");
                return "";
            }
        }
    }
}
#endif