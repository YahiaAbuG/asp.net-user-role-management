using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;
using WebApplication5.Attributes;
using System.IO.Compression;
using X.PagedList;
using X.PagedList.Extensions;
using WebApplication5.Models.Interfaces;

namespace WebApplication5.Controllers
{
    public class UserManagerController : Controller
    {
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISchoolRoleService _schoolRoleService;
        private readonly ICurrentSchoolService _currentSchoolService;

        public UserManagerController(UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            IMapper mapper, 
            IWebHostEnvironment webHostEnvironment,
            ISchoolRoleService schoolRoleService,
            ICurrentSchoolService currentSchoolService)
        {
            _mapper = mapper;
            _roleManager = roleManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _schoolRoleService = schoolRoleService;
            _currentSchoolService = currentSchoolService;
        }
        [Authorize]
        public async Task<IActionResult> Index(int? page)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = _mapper.Map<List<UserRolesViewModel>>(users);

            foreach (var userViewModel in userRolesViewModel)
            {
                var user = users.First(u => u.Id == userViewModel.UserId);
                userViewModel.Roles = await _schoolRoleService.GetUserRolesAsync(user.Id, schoolId);
            }

            var pagedUsers = userRolesViewModel.ToPagedList(pageNumber, pageSize);
            return View(pagedUsers);
        }


        // GET
        [AuthorizeSchoolRole("Admin,Manager")]
        public async Task<IActionResult> Manage(string userId)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            var isUserAdmin = await _schoolRoleService.IsUserInRoleAsync(user.Id, "Admin", schoolId);
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.isCurrentUserAdmin = await _schoolRoleService.IsUserInRoleAsync(currentUser.Id, "Admin", schoolId);

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
                roleViewModel.Selected = await _schoolRoleService.IsUserInRoleAsync(user.Id, roleViewModel.RoleName, schoolId);
            }

            return View(model);
        }


        // POST
        [AuthorizeSchoolRole("Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId, int schoolId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            // Validate that at least one role is selected (optional)
            if (!model.Any(r => r.Selected))
            {
                ModelState.AddModelError("", "You must select at least one role.");
                return View(model);
            }

            // Assign roles using your SchoolRoleService
            var selectedRoles = model
                .Where(x => x.Selected)
                .Select(x => x.RoleName);

            await _schoolRoleService.AssignRolesAsync(user.Id, selectedRoles, schoolId);

            return RedirectToAction("Index", new { schoolId });
        }


        // GET
        [AuthorizeSchoolRole("Admin,Manager")]
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
        [AuthorizeSchoolRole("Admin,Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return RedirectToAction(nameof(Index), new { schoolId });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Error deleting user");
                return View(user);
            }

            return RedirectToAction("Index", new { schoolId });
        }


        [AuthorizeSchoolRole("Admin")]
        public async Task<IActionResult> Create()
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View();
        }


        [AuthorizeSchoolRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            int schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;
            ViewBag.CurrentSchoolId = schoolId;

            if (ModelState.IsValid)
            {
                var user = _mapper.Map<ApplicationUser>(model);
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _schoolRoleService.AssignRolesAsync(user.Id, new[] { model.Role }, schoolId);
                    }

                    user.GenerateQrCode();
                    return RedirectToAction(nameof(Index), new { schoolId });
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
        [AuthorizeSchoolRole("Admin")]
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
                var fileExtension = Path.GetExtension(model.ProfileImage.FileName);
                var fileName = model.Id + fileExtension;
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                var newPath = Path.Combine("~/images", fileName).Replace("\\", "/");

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

        [AuthorizeSchoolRole("Admin,Manager")]
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

        [AuthorizeSchoolRole("Admin")]
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

        [AuthorizeSchoolRole("Admin,Manager")]
        public IActionResult SelectImages()
        {
            var imagesFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(imagesFolder))
            {
                ViewBag.ErrorMessage = "Images folder not found.";
                return View("NotFound");
            }

            var imageFiles = Directory.GetFiles(imagesFolder).Select(Path.GetFileName).ToList();
            var model = new DownloadImagesViewModel { ImageFileNames = imageFiles };

            return View(model);
        }

        [AuthorizeSchoolRole("Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadSelectedImages(DownloadImagesViewModel model)
        {
            if (model.ImageFileNames == null || !model.ImageFileNames.Any())
            {
                ModelState.AddModelError("", "No images selected.");
                return View("SelectImages", model);
            }

            var imagesFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            var zipFileName = "selected_images.zip";
            var zipFilePath = Path.Combine(_webHostEnvironment.WebRootPath, zipFileName);

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var fileName in model.ImageFileNames)
                {
                    var filePath = Path.Combine(imagesFolder, fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        zipArchive.CreateEntryFromFile(filePath, fileName);
                    }
                }
            }

            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(zipFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            System.IO.File.Delete(zipFilePath);

            return File(memoryStream, "application/zip", zipFileName);
        }

        [AuthorizeSchoolRole("Admin")]
        public IActionResult SelectQrs()
        {
            var qrsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "qrs");
            if (!Directory.Exists(qrsFolder))
            {
                ViewBag.ErrorMessage = "QRs folder not found.";
                return View("NotFound");
            }

            var qrFiles = Directory.GetFiles(qrsFolder).Select(Path.GetFileName).ToList();
            var model = new DownloadQrsViewModel { QrFileNames = qrFiles };

            return View(model);
        }

        [AuthorizeSchoolRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadSelectedQrs(DownloadQrsViewModel model)
        {
            if (model.QrFileNames == null || !model.QrFileNames.Any())
            {
                ModelState.AddModelError("", "No QR codes selected.");
                return View("SelectQrs", model);
            }

            var qrsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "qrs");
            var zipFileName = "selected_qrs.zip";
            var zipFilePath = Path.Combine(_webHostEnvironment.WebRootPath, zipFileName);

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var fileName in model.QrFileNames)
                {
                    var filePath = Path.Combine(qrsFolder, fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        zipArchive.CreateEntryFromFile(filePath, fileName);
                    }
                }
            }
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(zipFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            System.IO.File.Delete(zipFilePath);

            return File(memoryStream, "application/zip", zipFileName);
        }
    }
}
