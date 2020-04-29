using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tono;
using tSecretCommon;

namespace tSecretUwp
{
    public class AuthenticatorAzureAD : Authenticator
    {
        private readonly static System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
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
                        .WithAuthority(param.AuthorityAudience)
                        .WithUseCorporateNetwork(true)
                        .WithRedirectUri(param.PublicClientRedirectUri)
                        .Build();
            }
        }

        public override async Task<bool> LoginSilentAsync(Func<CancellationToken> token, Func<StreamWriter> log)
        {
            Initialize();
            var ret = false;
            try
            {
                var accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
                var firstAccount = accounts.FirstOrDefault();
                authResult = await clientApp.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync(token()); // Login automatically
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
                var lw = log?.Invoke();
                lw?.WriteLine($"Acquiring Token Silently:");
                lw?.WriteLine(ex.Message);
                lw?.WriteLine("(E101)");
            }
            return ret;
        }

        public override async Task<bool> LoginInteractiveAsync(Func<CancellationToken> token, Func<StreamWriter> log)
        {
            Initialize();
            var ret = false;
            try
            {
                authResult = await clientApp.AcquireTokenInteractive(scopes)
                                            .ExecuteAsync(token())
                                            .ConfigureAwait(false);
                IsAuthenticated = true;
                ret = true;
            }
            catch (MsalException msalex)
            {
                authResult = null;
                var lw = log?.Invoke();
                lw?.WriteLine($"Acquiring Token Interactive:");
                lw?.WriteLine(msalex.Message);
                lw?.WriteLine("(E102)");
            }
            catch (Exception ex)
            {
                authResult = null;
                var lw = log?.Invoke();
                lw?.WriteLine($"Acquiring Token Interactive:");
                lw?.WriteLine(ex.Message);
                lw?.WriteLine("(E101)");
            }
            return ret;
        }
        public override async Task<bool> LogoutAsync(Func<CancellationToken> token, Func<StreamWriter> log)
        {
            var ret = false;

            if (IsAuthenticated)
            {
                try
                {
                    var accounts = await clientApp.GetAccountsAsync().ConfigureAwait(false);
                    var firstAccount = accounts.FirstOrDefault();
                    await clientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
                    IsAuthenticated = false;
                }
                catch (MsalException ex)
                {
                    var lw = log?.Invoke();
                    lw?.WriteLine($"Signing-out user:");
                    lw?.WriteLine(ex.Message);
                    lw?.WriteLine("(E114)");

                }
            }
            return ret;
        }

        public override async Task<bool> GetPrivacyDataAsync(Func<CancellationToken> token, Func<StreamWriter> log)
        {
            if (IsAuthenticated && authResult != null)
            {
                var param = new MySecretParameter();
                var content = await GetHttpContentWithTokenAsync(param.GraphAPIEndpoint, authResult.AccessToken, token(), log?.Invoke()).ConfigureAwait(false);

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
                var lw = log?.Invoke();
                lw?.WriteLine($"Warning : Could not load privacy data from Microsoft Graph");
                lw?.WriteLine("(E115)");
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
