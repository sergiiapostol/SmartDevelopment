using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using SmartDevelopment.SampleApp.AspCore.Configuration;
using SmartDevelopment.SampleApp.AspCore.Models;
using IdentityUser = SmartDevelopment.Identity.Entities.IdentityUser;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/Account")]
    public sealed class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtTokenConfiguration _configuration;
        private readonly JwtSecurityTokenHandler _tokenHandler;


        public AccountController(
            IOptions<JwtTokenConfiguration> configuration,
            UserManager<IdentityUser> userManager,
            JwtSecurityTokenHandler tokenHandler)
        {
            _userManager = userManager;
            _tokenHandler = tokenHandler;
            _configuration = configuration.Value;
        }

        [HttpPost, Route("RegisterGuest")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody]RegistrationModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser
            {
                Id = ObjectId.GenerateNewId(),
                UserName = model.Email,
                NormalizedUserName = _userManager.NormalizeName(model.Email),
                Email = model.Email,
                NormalizedEmail = _userManager.NormalizeEmail(model.Email)
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddPasswordAsync(user, model.Password);
                if (result.Succeeded)
                    return Ok();
            }
            return StatusCode(500, result.Errors);
        }

        [HttpPost, Route("Token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResult), 200)]
        public async Task<IActionResult> Post([FromBody]TokenInput model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
                return NotFound();

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized();

            var token = await GetJwtSecurityToken(user);

            var response = new TokenResult
            {
                AccessToken = _tokenHandler.WriteToken(token),
                TokenType = JwtBearerDefaults.AuthenticationScheme,
                ExpiresAt = token.Payload.ValidTo
            };
            return new OkObjectResult(response);
        }

        private async Task<JwtSecurityToken> GetJwtSecurityToken(IdentityUser user)
        {
            const string signinAlgorithm = SecurityAlgorithms.HmacSha256;

            var userClaims = await _userManager.GetClaimsAsync(user);

            return new JwtSecurityToken(
                _configuration.Issuer,
                _configuration.Audience,
                GetTokenClaims(user).Union(userClaims),
                expires: DateTime.UtcNow.AddSeconds(_configuration.ExpireInSec),
                signingCredentials: new SigningCredentials(_configuration.SecurityKey, signinAlgorithm)
            );
        }

        private static List<Claim> GetTokenClaims(IdentityUser user)
        {
            return
            [
                new(JwtRegisteredClaimNames.UniqueName, user.Id.ToString())
            ];
        }
    }
}