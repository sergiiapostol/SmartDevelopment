using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Dal.Abstractions;
using SmartDevelopment.Dal.Abstractions.Models;
using SmartDevelopment.Logging;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly IDal<Identity.Entities.IdentityUser> _dal;

        public TestController(ILogger<TestController> logger, IDal<Identity.Entities.IdentityUser> dal)
        {
            _logger = logger;
            _dal = dal;
        }

        [HttpGet, Route("Logger")]
        public async Task<ActionResult> Logger()
        {
            _logger.Debug("Debug");
            _logger.Debug(new Exception("Debug"));
            _logger.Exception(new Exception("exception"));
            _logger.Information("Information");
            _logger.Trace("Trace");
            _logger.Warning(new Exception("Warning"));
            _logger.Warning("Warning");

            await _dal.SetAsync<Identity.Entities.IdentityUser>(v => v.CreatedAt > DateTime.UtcNow.Date, new List<PropertyUpdate<Identity.Entities.IdentityUser>> {
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.Email, "test@bla.com"),
                new PropertyUpdate<Identity.Entities.IdentityUser>(v=>v.SecurityStamp, "test@bla.com")
            }).ConfigureAwait(false);

            return Ok();
        }
    }
}