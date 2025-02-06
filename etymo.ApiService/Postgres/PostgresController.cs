using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace etymo.ApiService.Postgres
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PostgresController(PostgresService PostgresService) : ControllerBase
    {
        private readonly PostgresService _PostgresService = PostgresService;

        [HttpGet("word-list-overviews")]
        public async Task<IActionResult> GetWordListOverviewsByUserIdAsync([FromBody] Guid userId)
        {
            var wordListOverviews = await _PostgresService.SelectWordListOverviewsByUserIdAsync(userId);
            if (wordListOverviews == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(wordListOverviews);
            }
        }

        [HttpPut("word-list-overview")]
        public async Task<IActionResult> UpsertWordListOverviewAsync([FromBody] WordListOverview wordListOverview)
        {
            if (wordListOverview == null)
            {
                return BadRequest("Word list overview cannot be null.");
            }

            var rowsAffected = await _PostgresService.InsertWordListOverviewAsync(wordListOverview);

            return Ok(rowsAffected);
        }

        [HttpPut("word-list")]
        public async Task<IActionResult> UpsertWordListAsync([FromBody] WordList wordList)
        {
            if (wordList == null)
            {
                return BadRequest("Word list cannot be null.");
            }

            var rowsAffected = await _PostgresService.InsertWordListAsync(wordList);

            return Ok(rowsAffected);
        }

        [HttpDelete("word-list-overview")]
        public async Task<IActionResult> DeleteWordListOverviewAsync([FromBody] Guid guid)
        {
            bool rowsDeleted = await _PostgresService.DeleteWordListOverviewByGuidAsync(guid);

            if (rowsDeleted)
            {
                return NoContent(); // Successful delete operation.
            }

            return NotFound();
        }
    }
}