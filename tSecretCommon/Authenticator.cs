// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace tSecretCommon
{
    public abstract class Authenticator
    {
        public virtual string UserObjectID => "";

        public abstract Task<bool> LoginAsync();

        public abstract Task<string> GetPrivacyData();
        public bool IsAuthenticated { get; protected set; }
    }
}