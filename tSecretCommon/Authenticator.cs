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
        public bool IsAuthenticated { get; protected set; }

        public abstract Task<bool> LoginSilentAsync(Func<StoryNode> token);
        public abstract Task<bool> LoginInteractiveAsync(Func<StoryNode> token);
        public abstract Task<bool> LogoutAsync(Func<StoryNode> token);
        public abstract Task<bool> GetPrivacyDataAsync(Func<StoryNode> token);

    }
}