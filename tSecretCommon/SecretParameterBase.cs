// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

namespace tSecretCommon
{
    public class SecretParameterBase
    {
        /// <summary>
        /// Rijndael key
        /// </summary>
        public virtual string KEY => "1234567890123456";

        /// <summary>
        /// Usable text shuffle
        /// </summary>
        public virtual string TEXTSET64 => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+";

        /// <summary>
        /// 8[bit] * 16[char] = 128[bit]
        /// </summary>
        public virtual int IVNPP => 16;

        /// <summary>
        /// Azure Blob Storage Connection String for sync data
        /// </summary>
        public virtual string  AzureStorageConnectionString => "DefaultEndpointsProtocol=https;AccountName=XXXXXXX;AccountKey=XXXXXX/XXX+XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX/XXXXXXXXXXXXXXXXX==;EndpointSuffix=core.windows.net";

        /// <summary>
        /// AzureAD Application Client ID
        /// </summary>
        public virtual string AzureADClientId => "12345678-1234-1234-1234-1234567890ab";

        /// <summary>
        /// Authority Audience URI
        /// </summary>
        public virtual string AuthorityAudience => "https://login.microsoftonline.com/common";

        /// <summary>
        /// Public client redirect URI
        /// </summary>
        public virtual string PublicClientRedirectUri => "https://login.microsoftonline.com/common/oauth2/nativeclient";

        /// <summary>
        /// Microsoft Graph API endpoint URI
        /// </summary>
        public virtual string GraphAPIEndpoint => "https://graph.microsoft.com/v1.0/me";

        /// <summary>
        /// for Xamarin.Forms
        /// </summary>
        public virtual string IosKeychainSecurityGroups => "com.tomarika.tsecret";
    }

    /// <summary>
    /// Private secret key (Please override key texts for your secret)
    /// </summary>
    public partial class MySecretParameter : SecretParameterBase
    {
    }
}
