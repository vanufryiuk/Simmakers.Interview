using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Simmakers.Interview.Data.Models;

namespace Simmakers.Interview.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public AppUser[]? Users { get; set; }

        public IndexModel(UserManager<AppUser> userManager, ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public void OnGet()
        {
            Users = _userManager.Users.ToArray();
        }
    }
}