using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("Full")]
public class FullPipelineController : ControllerBase
{
    
}