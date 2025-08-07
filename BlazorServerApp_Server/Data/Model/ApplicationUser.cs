using Microsoft.AspNetCore.Identity;

namespace BlazorServerApp_Server.Data.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
    }
}
