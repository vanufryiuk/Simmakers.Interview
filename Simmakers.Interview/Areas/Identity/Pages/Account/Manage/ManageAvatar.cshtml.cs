using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Simmakers.Interview.Areas.Identity.Services;
using Simmakers.Interview.Data.Models;

namespace Simmakers.Interview.Areas.Identity.Pages.Account.Manage
{
    public class ManageAvatarModel : PageModel
    {
        public string[] ExistingImages { get; set; } = Array.Empty<string>();

        public string UserId { get; set; } = string.Empty;

        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ManageAvatarModel> _logger;
        private readonly ScopedFileManager _fileManager;

        public ManageAvatarModel(
            UserManager<AppUser> userManager,
            ScopedFileManager pictureManager,
            ILogger<ManageAvatarModel> logger)
        {
            _userManager = userManager;
            _fileManager = pictureManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            ExistingImages = (await _fileManager.ListScopeAsync(user.Id)).ToArray();
            UserId = user.Id;

            return Page();
        }

        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile uploadedImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (uploadedImage == null)
            {
                return BadRequest($"Uploaded image was not found.");
            }

            // TODO: this logic should be moved to File.cshtml
            using var uploadStream = uploadedImage.OpenReadStream();
            await _fileManager.StoreFileAsync(user.Id, uploadedImage.FileName, uploadedImage.ContentType, uploadStream, uploadedImage.Length);

            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(string imageName)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // TODO: this logic should be moved to File.cshtml
            await _fileManager.DeleteFileAsync(user.Id, imageName);

            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostSelectImageAsync(string imageName)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            user.AvatarImageName = imageName;

            await _userManager.UpdateAsync(user);

            return await OnGetAsync();
        }
    }
}
