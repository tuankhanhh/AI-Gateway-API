using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.DTO.Usage;
using MyWebApi.Service;
using System.Security.Claims;

namespace MyWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsageController : ControllerBase
    {
        private readonly IUsageService _usageService;

        public UsageController(IUsageService usageService)
        {
            _usageService = usageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyUsage()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { Message = "User ID not found in token" });
            }

            var usage = await _usageService.GetMyUsageAsync(userId);
            return Ok(usage);
        }
    }
}
