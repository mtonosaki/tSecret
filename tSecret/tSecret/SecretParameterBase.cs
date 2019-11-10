namespace tSecret
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
    }

    /// <summary>
    /// Private secret key (Please override key texts for your secret)
    /// </summary>
    public partial class MySecretParameter : SecretParameterBase
    {
    }
}
