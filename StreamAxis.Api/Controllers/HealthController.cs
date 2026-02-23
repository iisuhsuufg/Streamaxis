using Microsoft.AspNetCore.Mvc;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    
    [HttpGet("check")]
    public IActionResult Check()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            message = "StreamAxis API is running" 
        });
    }
}