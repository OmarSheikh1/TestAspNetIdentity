using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TestAspNetIdentity.Dtos;
using TestAspNetIdentity.Helper;
using TestAspNetIdentity.Models;

namespace TestAspNetIdentity.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private RoleManager<IdentityRole> roleManager { get; }

        public AccountController(UserManager<IdentityUser> userManager
            , SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }
        [HttpPost]
        [Route("Register")]
        //POST : /api/Account/Register
        public async Task<RegisterUserDto> Register(RegisterUserDto model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };
                var result = await userManager.CreateAsync(user, model.Password);
                IdentityResult roleResult;
                bool adminRoleExists = await roleManager.RoleExistsAsync(model.Role);
                if (!adminRoleExists)
                {
                    var adminRole = new IdentityRole(model.Role);
                    roleResult = await roleManager.CreateAsync(adminRole);
                }

                await userManager.AddToRoleAsync(user, model.Role);
                await userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, CRMPermissions.Read));
                //await userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, CRMPermissions.Update));
                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return model;
        }

        [HttpPost]
        [Route("Login")]
        //POST : /api/Account/Login
        public async Task<IActionResult> Login(LoginDto model)
        {
            var user = await userManager.FindByNameAsync(model.Username);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                //Get Roles assigned to User
                var role = await userManager.GetRolesAsync(user);

                //Get Claims assigned to User
                var claimsPrincipal = await signInManager.CreateUserPrincipalAsync(user);
               var userClaims = claimsPrincipal.Claims.ToList();

                try
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier,model.Username.ToString()),
                        new Claim(ClaimTypes.Role,role.FirstOrDefault())

                    }.Union(userClaims);
                    

                    var key = new SymmetricSecurityKey(Encoding.UTF8
                                  .GetBytes("super secret key"));

                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = DateTime.Now.AddDays(1),
                        SigningCredentials = creds
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();

                    var securityToken = tokenHandler.CreateToken(tokenDescriptor);

                    var token = tokenHandler.WriteToken(securityToken);

                    return Ok(new { token });

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                ;
            }
            else
            {
                return BadRequest(new { message = "Username or Password is incorrect." });
            }



        }
    }
}