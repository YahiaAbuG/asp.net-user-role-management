using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication5.Models.ViewModels;
using WebApplication5.Models;
using WebApplication5.Services;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace WebApplication5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService, IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var token = _jwtTokenService.GenerateAccessToken(user, userRoles);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                await _jwtTokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

                return Ok(new
                {
                    token,
                    refreshToken,
                    expiration = DateTime.Now.AddMinutes(30)
                });
            }
            return Unauthorized();
        }

        // POST: api/Auth/RefreshToken
        [HttpPost("RefreshToken")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenViewModel model)
        {
            if (ModelState.IsValid)
            {
                var principal = GetPrincipalFromExpiredToken(model.Token);
                if (principal == null)
                {
                    return BadRequest("Invalid token");
                }

                var userName = principal.Identity.Name;
                var user = await _userManager.FindByNameAsync(userName);
                if (user == null)
                {
                    return BadRequest("Invalid user");
                }

                var isValidRefreshToken = await _jwtTokenService.ValidateRefreshTokenAsync(user.Id, model.RefreshToken);
                if (!isValidRefreshToken)
                {
                    return BadRequest("Invalid refresh token");
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var newToken = _jwtTokenService.GenerateAccessToken(user, userRoles);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

                await _jwtTokenService.SaveRefreshTokenAsync(user.Id, newRefreshToken);

                return Ok(new
                {
                    token = newToken,
                    refreshToken = newRefreshToken,
                    expiration = DateTime.Now.AddMinutes(30)
                });
            }
            return BadRequest(ModelState);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"])),
                ValidateLifetime = false // Here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
