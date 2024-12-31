using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;
using System.IO.Compression;
using X.PagedList;
using X.PagedList.Extensions;

namespace WebApplication5.Controllers
{
    public class UserManagerController : Controller
    {
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserManagerController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            _mapper = mapper;
            _roleManager = roleManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }
        [Authorize]
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = _mapper.Map<List<UserRolesViewModel>>(users);

            foreach (var userViewModel in userRolesViewModel)
            {
                var user = users.First(u => u.Id == userViewModel.UserId);
                userViewModel.Roles = await GetUserRoles(user);
            }

            var pagedUsers = userRolesViewModel.ToPagedList(pageNumber, pageSize);
            return View(pagedUsers);
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }
        // GET
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            // Check if the user being managed has the "Admin" role
            var isUserAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // Check if the current user is an admin
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.isCurrentUserAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // If the current user is not an admin and the user being managed is an admin, deny access
            if (isUserAdmin && !ViewBag.isCurrentUserAdmin)
            {
                return Forbid();
            }

            ViewBag.UserName = user.UserName;

            var roles = await _roleManager.Roles.ToListAsync();

            ViewBag.AdminRoleId = (await _roleManager.FindByNameAsync("Admin"))?.Id;

            var model = _mapper.Map<List<ManageUserRolesViewModel>>(roles);

            foreach (var roleViewModel in model)
            {
                roleViewModel.Selected = await _userManager.IsInRoleAsync(user, roleViewModel.RoleName);
            }

            return View(model);
        }

        // POST
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("Index");
        }

        // GET
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            return View(user);
        }

        // POST
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Error deleting user");
                return View(user);
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _mapper.Map<ApplicationUser>(model);
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    user.GenerateQrCode();

                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View(model);
        }

        // GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            var model = _mapper.Map<EditUserViewModel>(user);
            ViewBag.ProfileImagePath = user.ProfileImagePath;
            //var model = new EditUserViewModel
            //{
            //    Id = user.Id,
            //    Email = user.Email,
            //    UserName = user.UserName,
            //    FirstName = user.FirstName,
            //    LastName = user.LastName
            //};
            return View(model);
        }

        // POST
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {model.Id} cannot be found";
                return View("NotFound");
            }

            // Check if the username already exists
            var userWithSameUserName = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.Id != model.Id);
            if (userWithSameUserName != null)
            {
                ModelState.AddModelError("UserName", "Username already exists");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.ProfileImage != null)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                var newPath = Path.Combine("~/images", uniqueFileName).Replace("\\", "/");

                // Save the relative path to the user's profile
                user.ProfileImagePath = newPath;
            }

            _mapper.Map(model, user);
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DownloadImages()
        {
            var imagesFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(imagesFolder))
            {
                ViewBag.ErrorMessage = "Images folder not found.";
                return View("NotFound");
            }

            var zipFileName = "images.zip";
            var zipFilePath = Path.Combine(_webHostEnvironment.WebRootPath, zipFileName);

            // Create the zip file
            ZipFile.CreateFromDirectory(imagesFolder, zipFilePath);

            // Read the zip file into a memory stream
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(zipFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            // Delete the zip file from the server after reading it
            System.IO.File.Delete(zipFilePath);

            // Return the zip file as a download
            return File(memoryStream, "application/zip", zipFileName);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadQrs()
        {
            var qrsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "qrs");
            if (!Directory.Exists(qrsFolder))
            {
                ViewBag.ErrorMessage = "QRs folder not found.";
                return View("NotFound");
            }

            var zipFileName = "qrs.zip";
            var zipFilePath = Path.Combine(_webHostEnvironment.WebRootPath, zipFileName);

            // Create the zip file
            ZipFile.CreateFromDirectory(qrsFolder, zipFilePath);

            // Read the zip file into a memory stream
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(zipFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            // Delete the zip file from the server after reading it
            System.IO.File.Delete(zipFilePath);

            // Return the zip file as a download
            return File(memoryStream, "application/zip", zipFileName);
        }
    }
}
