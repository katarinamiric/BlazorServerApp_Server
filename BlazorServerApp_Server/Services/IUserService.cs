using BlazorServerApp_Server.Data;

namespace BlazorServerApp_Server.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetCurrentUserAsync();
    }
}
