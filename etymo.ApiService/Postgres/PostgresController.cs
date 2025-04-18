using etymo.ApiService.Postgres.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Security.Claims;

namespace etymo.ApiService.Postgres
{
    [ApiController]
    [Route("[controller]")]
    public class PostgresController(PostgresService postgresService) : ControllerBase
    {
        private readonly PostgresService _postgresService = postgresService;

        [HttpGet("word-list-overview")]
        public async Task<IActionResult> FetchWordListOverviewAsync([FromQuery] Guid wordListOverviewId)
        {
            var wordListOverview = await _postgresService.FetchWordListOverviewAsync(wordListOverviewId);
            if (wordListOverview == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordListOverview);
            }
        }

        [HttpGet("word-list-overviews")]
        public async Task<IActionResult> GetWordListOverviewsAsync(
            [FromQuery] Guid? userId = null,
            [FromQuery] DateRange? dateRange = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate pagination parameters
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50); // Limit maximum page size

            // Get the current user's GUID
            Guid? currentUserGuid = null;

            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Get the user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var guid))
                {
                    currentUserGuid = guid;
                }
            }

            var wordListOverviews = await _postgresService.SelectPublicWordListOverviewsAsync(
                userId,
                dateRange,
                pageNumber,
                pageSize,
                currentUserGuid); // Pass the current user's GUID

            if (wordListOverviews == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordListOverviews);
            }
        }

        [HttpGet("word-list")]
        public async Task<IActionResult> FetchWordListAsync([FromQuery] Guid wordListId)
        {
            var wordList= await _postgresService.FetchWordListAsync(wordListId);
            if (wordList== null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordList);
            }
        }

        [CreatorOrAdmin]
        [HttpGet("private-word-list")]
        public async Task<IActionResult> FetchPrivateWordListAsync([FromQuery] Guid wordListId, [FromQuery] Guid userId)
        {
            var wordList = await _postgresService.FetchPrivateWordListAsync(wordListId, userId);
            if (wordList == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordList);
            }
        }

        [CreatorOrAdmin]
        [HttpGet("private-word-list-overview")]
        public async Task<IActionResult> FetchPrivateWordListOverviewAsync([FromQuery] Guid wordListOverviewId, [FromQuery] Guid userId)
        {
            var wordListOverview = await _postgresService.FetchPrivateWordListOverviewAsync(wordListOverviewId, userId);
            if (wordListOverview == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordListOverview);
            }
        }

        [CreatorOrAdmin]
        [HttpGet("private-word-list-overviews")]
        public async Task<IActionResult> GetPrivateWordListOverviewsAsync(
            [FromQuery] Guid userId,
            [FromQuery] DateRange? dateRange = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate pagination parameters
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50); // Limit maximum page size

            var wordListOverviews = await _postgresService.SelectPrivateWordListOverviewsAsync(
                userId,
                dateRange,
                pageNumber,
                pageSize);

            if (wordListOverviews == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordListOverviews);
            }
        }

        [CreatorOrAdmin]
        [HttpPut("word-list-overview")]
        public async Task<IActionResult> UpsertWordListOverviewAsync([FromBody] WordListOverview wordListOverview)
        {
            if (wordListOverview == null)
            {
                return BadRequest("Word list overview cannot be null.");
            }

            var rowsAffected = await _postgresService.InsertWordListOverviewAsync(wordListOverview);

            return Ok(rowsAffected);
        }

        [CreatorOrAdmin]
        [HttpPut("word-list")]
        public async Task<IActionResult> UpsertWordListAsync([FromBody] WordList wordList)
        {
            if (wordList == null)
            {
                return BadRequest("Word list cannot be null.");
            }

            var rowsAffected = await _postgresService.InsertWordListAsync(wordList);

            return Ok(rowsAffected);
        }

        [CreatorOrAdmin]
        [HttpDelete("word-list-overview")]
        [ValidateCustomAntiForgeryToken]
        public async Task<IActionResult> DeleteWordListOverviewAsync([FromQuery] Guid wordlistOverviewId, [FromQuery] Guid userId)
        {
            bool rowsDeleted = await _postgresService.DeleteWordListOverviewByGuidAsync(wordlistOverviewId, userId);

            if (rowsDeleted)
            {
                return NoContent(); // Successful delete operation.
            }

            return NotFound();
        }

        [HttpPost("toggle-upvote")]
        [Authorize]
        [ValidateCustomAntiForgeryToken]
        public async Task<IActionResult> ToggleUpvoteAsync([FromQuery] Guid wordListOverviewId)
        {
            // Get the current user's GUID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserGuid))
            {
                return Unauthorized();
            }

            // Toggle the upvote
            var result = await _postgresService.ToggleUpvoteAsync(currentUserGuid, wordListOverviewId);

            // Return the new upvote count and status
            var upvoteCount = await _postgresService.GetUpvoteCountAsync(wordListOverviewId);

            return Ok(new
            {
                isUpvoted = result,
                upvoteCount
            });
        }
    }
}