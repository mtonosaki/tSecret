using Android;
using Android.Content.PM;
using Android.Hardware.Fingerprints;
using Android.Support.V4.Content;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Util;
using Java.Lang;
using Javax.Crypto;
using System;
using System.Threading.Tasks;
using tSecretXamarin.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(AuthService))]
namespace tSecretXamarin.Droid
{
    public class AuthService : IAuthService
    {
        public Task<bool> GetAuthentication()
        {
            return Authenticate();
        }

        private async Task<bool> Authenticate()
        {
            SimpleAuthCallbacks callback = new SimpleAuthCallbacks();
            Android.Content.Context context = Android.App.Application.Context;
            Permission isUsingFingerprint = ContextCompat.CheckSelfPermission(context, Manifest.Permission.UseFingerprint);
            if (isUsingFingerprint == Permission.Granted)
            {
                Android.Support.V4.OS.CancellationSignal cancellationSignal = new Android.Support.V4.OS.CancellationSignal();
                CryptoObjectHelper cryptHelper = new CryptoObjectHelper();
                FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.From(context);
                fingerprintManager.Authenticate(
                    cryptHelper.BuildCryptoObject(),
                    (int)FingerprintAuthenticationFlags.None, /* flags */
                    cancellationSignal,
                    callback,
                    null
                );
            }

            while (callback.IsWaiting)
            {
                await Task.Delay(87);
            }

            return callback.Status == SimpleAuthCallbacks.AuthenticateResult.Success;
        }
        private class SimpleAuthCallbacks : FingerprintManagerCompat.AuthenticationCallback
        {
            public enum AuthenticateResult
            {
                NA,
                Waiting,
                Success,
                ScanFailed,
                AuthenticationFailed,
            }

            public AuthenticateResult Status { get; set; }
            public string ResultMessage { get; set; }

            public bool IsWaiting
            {
                get
                {
                    switch (Status)
                    {
                        case AuthenticateResult.NA:
                        case AuthenticateResult.Waiting:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly string TAG = "X:" + typeof(SimpleAuthCallbacks).Name;
            private static readonly byte[] SECRET_BYTES = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            public SimpleAuthCallbacks()
            {
                Status = AuthenticateResult.NA;
                ResultMessage = "";
            }

            public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
            {
                Log.Debug(TAG, "OnAuthenticationSucceeded");
                if (result.CryptoObject.Cipher != null)
                {
                    try
                    {
                        byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(SECRET_BYTES);
                        Log.Debug(TAG, $"Fingerprint authentication succeeded, doFinal results: {Convert.ToBase64String(doFinalResult)}");
                        Status = AuthenticateResult.Success;
                    }
                    catch (BadPaddingException bpe)
                    {
                        Log.Error(TAG, "Failed to encrypt the data with the generated key." + bpe);
                        Status = AuthenticateResult.ScanFailed;
                    }
                    catch (IllegalBlockSizeException ibse)
                    {
                        Log.Error(TAG, "Failed to encrypt the data with the generated key." + ibse);
                        Status = AuthenticateResult.AuthenticationFailed;
                    }
                }
                else
                {
                    Log.Debug(TAG, "Fingerprint authentication succeeded.");
                    Status = AuthenticateResult.Success;
                }
            }

            public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
            {
                // There are some situations where we don't care about the error. For example, 
                // if the user cancelled the scan, this will raise errorID #5. We don't want to
                // report that, we'll just ignore it as that event is a part of the workflow.
                Log.Debug(TAG, $"OnAuthenticationError: {errMsgId}:`{errString}`.");
                Status = AuthenticateResult.ScanFailed;
            }

            public override void OnAuthenticationFailed()
            {
                Log.Info(TAG, "Authentication failed.");
                Status = AuthenticateResult.AuthenticationFailed;
            }

            public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
            {
                Log.Debug(TAG, $"OnAuthenticationHelp: {helpString}:`{helpMsgId}`");
                Status = AuthenticateResult.ScanFailed;
            }
        }
    }
}