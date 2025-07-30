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
using WebApplication5.Data;

namespace WebApplication5.Controllers
{
    public class UserManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
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
            ICurrentSchoolService currentSchoolService,
            ApplicationDbContext context)
        {
            _context = context;
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

            // Filter users who belong to the current school
            var userIdsInSchool = await _context.UserRoles
                //.Where(ur => ur.SchoolId == schoolId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => userIdsInSchool.Contains(u.Id))
                .ToListAsync();

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
        [AuthorizeSchoolRole("Admin, Manager")]
        public async Task<IActionResult> Manage(string userId)
        {
            var schoolId = _currentSchoolService.GetCurrentSchoolId(HttpContext) ?? 1;

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            if (await _schoolRoleService.IsUserInRoleAsync(userId, "SuperAdmin", 0) && !(await _schoolRoleService.IsUserInRoleAsync(currentUserId, "SuperAdmin", 0)))
                return Forbid();

            if (await _schoolRoleService.IsUserInRoleAsync(currentUserId, "Manager", schoolId) && (await _schoolRoleService.IsUserInRoleAsync(userId, "Admin", schoolId)))
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return View("NotFound");

            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName
            };

            // General roles (SuperAdmin only)
            var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");
            model.GeneralRoles.Add(new RoleCheckboxViewModel
            {
                RoleId = superAdminRole.Id,
                RoleName = superAdminRole.Name,
                Selected = await _context.UserRoles.AnyAsync(r => r.UserId == user.Id && r.RoleId == superAdminRole.Id)
            });

            var rolesWithMeta = await _context.UserRoles
                .Include(r => r.School)
                .Include(r => r.Activity)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var userRoleDisplays = new List<UserRoleDisplayViewModel>();

            foreach (var role in rolesWithMeta)
            {
                var roleName = (await _roleManager.FindByIdAsync(role.RoleId))?.Name ?? "N/A";

                userRoleDisplays.Add(new UserRoleDisplayViewModel
                {
                    SchoolName = role.School?.Name ?? "N/A",
                    ActivityName = role.Activity?.Name ?? "N/A",
                    RoleName = roleName,
                    SchoolId = role.School?.Id,
                    RoleId = role.RoleId,
                    ActivityId = role.Activity?.Id
                });
            }

            model.UserRolesTable = userRoleDisplays
                .OrderBy(r => r.SchoolId == null && r.ActivityId == null ? 0 :
                              r.SchoolId != null && r.ActivityId == null ? 1 : 2)
                .ThenBy(r => r.SchoolName)
                .ThenBy(r => r.ActivityName)
                .ToList();

            // Populate form dropdowns
            model.Form.AvailableSchools = await _context.Schools
                .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                .ToListAsync();

            model.Form.AvailableRoles = await _roleManager.Roles
                .Where(r => r.Name != "SuperAdmin")
                .Select(r => new SelectListItem { Text = r.Name, Value = r.Name })
                .ToListAsync();

            model.Form.AvailableActivities = new List<SelectListItem>(); // Will be dynamically populated via AJAX

            return View(model);
        }


        // POST
        [AuthorizeSchoolRole("Admin,Manager")]
        [HttpPost]
        [ActionName("Manage")]
        public async Task<IActionResult> Manage(ManageRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return View("NotFound");

            // Update general role (SuperAdmin)
            var superAdmin = await _roleManager.FindByNameAsync("SuperAdmin");
            var hasSuperAdmin = await _context.UserRoles.AnyAsync(r => r.UserId == user.Id && r.RoleId == superAdmin.Id);

            if (model.GeneralRoles != null && model.GeneralRoles.Any())
            {
                var selected = model.GeneralRoles.First().Selected;

                if (selected && !hasSuperAdmin)
                {
                    _context.UserRoles.Add(new ApplicationUserRole { UserId = user.Id, RoleId = superAdmin.Id });
                }
                else if (!selected && hasSuperAdmin)
                {
                    var entry = await _context.UserRoles.FirstOrDefaultAsync(r => r.UserId == user.Id && r.RoleId == superAdmin.Id);
                    if (entry != null) _context.UserRoles.Remove(entry);
                }
            }

            // Assign new role
            if (!string.IsNullOrEmpty(model.Form.SelectedRoleName))
            {
                var role = await _roleManager.FindByNameAsync(model.Form.SelectedRoleName);

                var isActivityRole = model.Form.SelectedRoleName == "ActivityAdmin" || model.Form.SelectedRoleName == "ActivityMember";
                var selectedActivityId = isActivityRole ? model.Form.SelectedActivityId : null;

                var exists = await _context.UserRoles.AnyAsync(r =>
                    r.UserId == user.Id &&
                    r.RoleId == role.Id &&
                    r.SchoolId == model.Form.SelectedSchoolId &&
                    r.ActivityId == selectedActivityId);

                if (role != null)
                {

                    if (!exists)
                    {
                        var newRole = new ApplicationUserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id,
                            SchoolId = model.Form.SelectedSchoolId,
                            ActivityId = model.Form.SelectedRoleName is "ActivityAdmin" or "ActivityMember"
                                         ? model.Form.SelectedActivityId
                                         : null
                        };

                        _context.UserRoles.Add(newRole);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Manage", new { userId = model.UserId });
        }


        [HttpGet]
        public async Task<IActionResult> GetActivitiesBySchool(int schoolId)
        {
            var activities = await _context.Activity
                .Where(a => a.SchoolId == schoolId)
                .Select(a => new { a.Id, a.Name })
                .ToListAsync();

            return Json(activities);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUserRole(string userId, string roleId, int? schoolId, int? activityId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var role = await _roleManager.FindByIdAsync(roleId);

            var model = new DeleteUserRoleViewModel
            {
                UserId = userId,
                UserName = user?.UserName,
                RoleId = roleId,
                RoleName = role?.Name,
                SchoolId = schoolId,
                ActivityId = activityId,
                SchoolName = schoolId.HasValue ? (await _context.Schools.FindAsync(schoolId))?.Name : "N/A",
                ActivityName = activityId.HasValue ? (await _context.Activity.FindAsync(activityId))?.Name : "N/A"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserRoleConfirmed(DeleteUserRoleViewModel model)
        {
            var entry = await _context.UserRoles.FirstOrDefaultAsync(r =>
                r.UserId == model.UserId &&
                r.RoleId == model.RoleId &&
                r.SchoolId == model.SchoolId &&
                r.ActivityId == model.ActivityId);

            if (entry != null)
            {
                _context.UserRoles.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Manage", new { userId = model.UserId });
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
