using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.DTO.Conversation;
using MyWebApi.Service;

namespace MyWebApi.Controllers
{
    [Route("api/conversations/{conversationId}/messages")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
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
        public async Task<IActionResult> Create(int conversationId, [FromBody] MessageCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var response = await _messageService.CreateUserMessageAsync(userId, conversationId, request);

                if (response == null)
                {
                    return NotFound(); // Ownership check failed or not found -> return 404
                }

                return StatusCode(201, response); // 201 Created
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
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var messages = await _messageService.GetMessagesAsync(userId, conversationId);

                if (messages == null)
                {
                    return NotFound(); // Ownership check failed or not found -> return 404
                }

                return Ok(messages);
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
