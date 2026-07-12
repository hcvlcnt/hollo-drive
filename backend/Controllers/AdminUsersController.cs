using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = ApplicationRole.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public AdminUsersController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
        {
            var users = await _userManagementService.SearchAsync(searchTerm, includeDeleted, skip, take, cancellationToken);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var user = await _userManagementService.GetByIdAsync(id, includeDeleted: true, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<UserResponse>> Update(
            Guid id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId.HasValue && id == currentUserId.Value && request.IsActive == false)
            {
                return BadRequest(new { error = "The current admin user cannot deactivate itself." });
            }

            if (currentUserId.HasValue &&
                id == currentUserId.Value &&
                !string.IsNullOrWhiteSpace(request.Role) &&
                !string.Equals(request.Role, ApplicationRole.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "The current admin user cannot remove its own admin role." });
            }

            try
            {
                var user = await _userManagementService.UpdateAsync(id, request, cancellationToken);
                return user is null ? NotFound() : Ok(user);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId.HasValue && id == currentUserId.Value)
            {
                return BadRequest(new { error = "The current admin user cannot delete itself." });
            }

            var deleted = await _userManagementService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        private Guid? GetCurrentUserId()
        {
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(currentUserIdClaim, out var currentUserId) ? currentUserId : null;
        }
    }
}
