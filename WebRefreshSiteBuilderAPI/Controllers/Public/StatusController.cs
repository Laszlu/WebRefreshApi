using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshSiteBuilderAPI.Controllers.Public;

[AllowAnonymous]
[ApiController]
[Route("Status")]
public class StatusController : ControllerBase
{
    [HttpGet("Version")]
    public IActionResult GetVersion()
    {
        var version = VersionHelper.LoadVersionInfo();

        return Ok(version);
    }
}