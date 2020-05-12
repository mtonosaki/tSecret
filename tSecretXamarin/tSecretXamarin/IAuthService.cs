using System.Threading.Tasks;

namespace tSecretXamarin
{
    public interface IAuthService
    {
        Task<bool> GetAuthentication();
    }
}
