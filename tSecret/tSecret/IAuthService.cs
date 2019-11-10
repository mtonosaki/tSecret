using System.Threading.Tasks;

namespace tSecret
{
    public interface IAuthService
    {
        Task<bool> GetAuthentication();
    }
}