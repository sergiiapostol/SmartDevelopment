using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDevelopment.Logging;
using System;

namespace SmartDevelopment.SampleApp.AspCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LoggerController : ControllerBase
    {
        private readonly ILogger _logger;

        public LoggerController(ILogger<LoggerController> logger)
        {
            _logger = logger;
        }

        [HttpGet, Route("Logger")]
        public ActionResult Logger()
        {
            _logger.Debug("Debug");
            _logger.Debug(new Exception("Debug"));
            _logger.Exception(new Exception("exception"));
            _logger.Information("Information");
            _logger.Trace("Trace");
            _logger.Warning(new Exception("Warning"));
            _logger.Warning("Warning");

            return Ok();
        }
    }
}