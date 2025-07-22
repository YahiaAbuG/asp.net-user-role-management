using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UserApiController(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET: api/User/Users
        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid("User not authenticated.");

            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = _mapper.Map<List<UserRolesViewModel>>(users);

            foreach (var userViewModel in userRolesViewModel)
            {
                var user = users.First(u => u.Id == userViewModel.UserId);
                userViewModel.Roles = await GetUserRoles(user);
            }

            return Ok(userRolesViewModel);
        }

        // POST: api/User/AddUser
        [HttpPost("AddUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUser([FromBody] CreateUserViewModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid("User not authenticated.");

            if (ModelState.IsValid)
            {
                var user = _mapper.Map<ApplicationUser>(model);
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                        await _userManager.AddToRoleAsync(user, model.Role);
                    return Ok(user);
                }
                return BadRequest(result.Errors);
            }
            return BadRequest(ModelState);
        }

        // PUT: api/User/EditUser
        [HttpPut("EditUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser([FromBody] EditUserApiViewModel model)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid("User not authenticated.");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                    return NotFound();

                var profileImageFile = ConvertPathToIFormFile(user.ProfileImagePath);

                var newModel = new EditUserViewModel
                {
                    Id = model.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.UserName,
                    ProfileImage = profileImageFile
                };

                _mapper.Map(newModel, user);
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                    return Ok(user);

                return BadRequest(result.Errors);
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/User/DeleteUser/{id}
        [HttpDelete("DeleteUser/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid("User not authenticated.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Errors);
        }

        // POST: api/User/ManageUserRoles
        [HttpPost("ManageUserRoles")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageUserRoles([FromBody] List<ManageUserRolesViewModel> model, [FromQuery] string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid("User not authenticated.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            if (!currentUserRoles.Contains("Admin") && roles.Contains("Admin"))
                return BadRequest("Cannot modify roles for an Admin");

            foreach (var modelRole in model)
            {
                if (!currentUserRoles.Contains("Admin") &&
                    modelRole.RoleName == "Admin" &&
                    (modelRole.Selected != roles.Contains("Admin")))
                {
                    return BadRequest("Cannot modify Admin role");
                }
            }

            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
                return BadRequest("Cannot remove user existing roles");

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
                return BadRequest("Cannot add selected roles to user");

            return Ok();
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        private IFormFile ConvertPathToIFormFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return null;

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(filePath);
            var formFile = new FormFile(stream, 0, stream.Length, "ProfileImage", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg" // or detect MIME type dynamically if needed
            };

            return formFile;
        }
    }
}
