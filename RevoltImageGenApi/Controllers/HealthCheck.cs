using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RevoltImageGenApi.Controllers
{
    [Route("api/ping")]
    [ApiController]
    public class HealthCheck : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> checkHealth()
        {
            return Utils.getDatabaseStatus() ? Ok("Database connected") : Problem("Failed to query database");
        }
    }
}
