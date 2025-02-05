using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication5.Models;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
                    return Ok(user);
                }
                return BadRequest(result.Errors);
            }
            return BadRequest(ModelState);
        }

        // PUT: api/User/EditUser
        [HttpPut("EditUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser([FromBody] EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                _mapper.Map(model, user);
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Ok(user);
                }
                return BadRequest(result.Errors);
            }
            return BadRequest(ModelState);
        }

        // DELETE: api/User/DeleteUser/{id}
        [HttpDelete("DeleteUser/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Ok();
            }
            return BadRequest(result.Errors);
        }

        // POST: api/User/ManageUserRoles
        [HttpPost("ManageUserRoles")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageUserRoles([FromBody] List<ManageUserRolesViewModel> model, [FromQuery] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!User.IsInRole("Admin") && roles.Contains("Admin"))
            {
                return BadRequest("Cannot modify roles for an Admin");
            }

            foreach (var modelRole in model)
            {
                if (!User.IsInRole("Admin") && modelRole.RoleName == "Admin" && (modelRole.Selected != roles.Contains("Admin")))
                {
                    return BadRequest("Cannot modify Admin role");
                }
            }

            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                return BadRequest("Cannot remove user existing roles");
            }

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                return BadRequest("Cannot add selected roles to user");
            }

            return Ok();
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }
    }
}
