// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace tSecretCommon
{
    public abstract class Authenticator
    {
        public virtual string UserObjectID { get; protected set; }
        public virtual string DisplayName { get; protected set; }

        public abstract Task<bool> LoginSilentAsync(Func<CancellationToken> token, Func<StreamWriter> log);
        public abstract Task<bool> LoginInteractiveAsync(Func<CancellationToken> token, Func<StreamWriter> log);
        public abstract Task<bool> LogoutAsync(Func<CancellationToken> token, Func<StreamWriter> log);
        public abstract Task<bool> GetPrivacyDataAsync(Func<CancellationToken> token, Func<StreamWriter> log);

        public bool IsAuthenticated { get; protected set; }
    }
}