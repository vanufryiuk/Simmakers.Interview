using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Simmakers.Interview.Areas.Identity.Services;
using Simmakers.Interview.Data.Models;

namespace Simmakers.Interview.Areas.Identity.Pages.Account
{
    public class FileModel : PageModel
    {
        private readonly ILogger<FileModel> _logger;
        private readonly ScopedFileManager _fileManager;

        public FileModel(
            ScopedFileManager fileManager,
            ILogger<FileModel> logger)
        {
            _fileManager = fileManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync([FromQuery(Name = "name")] string fileName, [FromQuery(Name = "user")] string userId)
        {
            var image = await _fileManager.LoadFileAsync(userId, fileName);
            return File(image.Content, image.ContentType);
        }
    }
}
