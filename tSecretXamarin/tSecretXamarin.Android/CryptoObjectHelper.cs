using Android.Security.Keystore;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Util;
using Java.Security;
using Javax.Crypto;
using System;

namespace tSecretXamarin.Droid
{
    /// <summary>
    /// This class encapsulates the creation of a CryptoObject based on a javax.crypto.Cipher.
    /// </summary>
    /// <remarks>
    /// Each invocation of BuildCryptoObject will instantiate a new CryptoObjet. 
    /// If necessary a key for the cipher will be created.
    /// </remarks>
    public class CryptoObjectHelper
    {
        // ReSharper disable InconsistentNaming
        private static readonly string TAG = "X:" + typeof(CryptoObjectHelper).Name;

        // This can be key name you want. Should be unique for the app.
        private static readonly string KEY_NAME = "BasicFingerPrintSample.FingerprintManagerAPISample.sample_fingerprint_key";

        // We always use this keystore on Android.
        private static readonly string KEYSTORE_NAME = "AndroidKeyStore";

        // Should be no need to change these values.
        private static readonly string KEY_ALGORITHM = KeyProperties.KeyAlgorithmAes;
        private static readonly string BLOCK_MODE = KeyProperties.BlockModeCbc;
        private static readonly string ENCRYPTION_PADDING = KeyProperties.EncryptionPaddingPkcs7;
        private static readonly string TRANSFORMATION = KEY_ALGORITHM + "/" + BLOCK_MODE + "/" + ENCRYPTION_PADDING;

        // ReSharper restore InconsistentNaming
        private readonly KeyStore _keystore;

        public CryptoObjectHelper()
        {
            _keystore = KeyStore.GetInstance(KEYSTORE_NAME);
            _keystore.Load(null);
        }

        public FingerprintManagerCompat.CryptoObject BuildCryptoObject()
        {
            var cipher = CreateCipher();
            return new FingerprintManagerCompat.CryptoObject(cipher);
        }

        /// <summary>
        /// Creates the cipher.
        /// </summary>
        /// <returns>
        /// The cipher.
        /// </returns>
        /// <param name="retry">If set to <c>true</c>, recreate the key and try again.</param>
        private Cipher CreateCipher(bool retry = true)
        {
            var key = GetKey();
            var cipher = Cipher.GetInstance(TRANSFORMATION);
            try
            {
                cipher.Init(CipherMode.EncryptMode, key);
            }
            catch (KeyPermanentlyInvalidatedException e)
            {
                Log.Debug(TAG, "The key was invalidated, creating a new key.");
                _keystore.DeleteEntry(KEY_NAME);
                if (retry)
                {
                    CreateCipher(false);
                }
                else
                {
                    throw new Exception("Could not create the cipher for fingerprint authentication.", e);
                }
            }
            return cipher;
        }

        /// <summary>
        /// Will get the key from the Android keystore, creating it if necessary.
        /// </summary>
        /// <returns>
        /// </returns>
        private IKey GetKey()
        {
            if (!_keystore.IsKeyEntry(KEY_NAME))
            {
                CreateKey();
            }

            var secretKey = _keystore.GetKey(KEY_NAME, null);
            return secretKey;
        }

        /// <summary>
        /// Creates the Key for fingerprint authentication.
        /// </summary>
        private void CreateKey()
        {
            var keyGen = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KEYSTORE_NAME);
            var keyGenSpec =
                new KeyGenParameterSpec.Builder(KEY_NAME, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                    .SetBlockModes(BLOCK_MODE)
                    .SetEncryptionPaddings(ENCRYPTION_PADDING)
                    .SetUserAuthenticationRequired(true)
                    .Build();
            keyGen.Init(keyGenSpec);
            keyGen.GenerateKey();
            Log.Debug(TAG, "New key created for fingerprint authentication.");
        }
    }
}