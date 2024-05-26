using Microsoft.AspNetCore.Mvc;

namespace ArkProjects.Hath.WebService.Controllers.V1;

[ApiController]
[Route("/api/v1/[controller]")]
public class OkController : ControllerBase
{
    [HttpGet()]
    public ActionResult Get(CancellationToken ct = default)
    {
        return Ok();
    }
}