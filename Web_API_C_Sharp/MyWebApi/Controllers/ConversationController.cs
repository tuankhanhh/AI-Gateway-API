using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.DTO.Conversation;
using MyWebApi.Service;

namespace MyWebApi.Controllers
{
    [Route("api/conversations")]
    [ApiController]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid User ID in token.");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var response = await _conversationService.CreateAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversations = await _conversationService.GetMyConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversation = await _conversationService.GetByIdAsync(userId, id);

                if (conversation == null)
                {
                    return NotFound(); // 404
                }

                return Ok(conversation);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _conversationService.DeleteAsync(userId, id);

                if (!deleted)
                {
                    return NotFound();
                }

                return NoContent(); // 204
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
