using Microsoft.AspNetCore.Identity;

namespace Simmakers.Interview.Data.Models
{
    public class AppUser: IdentityUser
    {
        public string? AvatarImageName { get; set; }
    }
}
