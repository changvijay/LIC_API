using System.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeroSectionController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<HeroSectionController> _logger;

        public HeroSectionController(IDbConnection db, ILogger<HeroSectionController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetAll(int agentId)
        {
            try
            {
                var sql = @"SELECT id, 
                                   title, 
                                   subtitle, 
                                   action_text AS ActionText, 
                                   action_link AS ActionLink, 
                                   image_url AS ImageUrl 
                            FROM hero_section 
                            WHERE agent_id = @AgentId";

                var result = await _db.QueryAsync<HeroSection>(sql, new { AgentId = agentId });
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hero sections for agent {AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var sql = @"SELECT id, 
                                   title, 
                                   subtitle, 
                                   action_text AS ActionText, 
                                   action_link AS ActionLink, 
                                   image_url AS ImageUrl 
                            FROM hero_section 
                            WHERE id = @Id";

                var result = await _db.QueryFirstOrDefaultAsync<HeroSection>(sql, new { Id = id });
                if (result == null)
                {
                    return NotFound(new { status = 404, message = "Not Found" });
                }
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hero section {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(HeroSection model)
        {
            try
            {
                var sql = @"INSERT INTO hero_section 
                               (title, subtitle, action_text, action_link, image_url, agent_id) 
                            VALUES 
                               (@Title, @Subtitle, @ActionText, @ActionLink, @ImageUrl, @AgentId)";

                await _db.ExecuteAsync(sql, model);
                return Ok(new { status = 200, message = "Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hero section");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, HeroSection model)
        {
            try
            {
                var sql = @"UPDATE hero_section SET 
                                title = @Title,
                                subtitle = @Subtitle,
                                action_text = @ActionText, 
                                action_link = @ActionLink,
                                image_url = @ImageUrl
                            WHERE id = @Id";

                await _db.ExecuteAsync(sql, new
                {
                    model.Title,
                    model.Subtitle,
                    model.ActionText,
                    model.ActionLink,
                    model.ImageUrl,
                    Id = id
                });
                return Ok(new { status = 200, message = "Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hero section {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _db.ExecuteAsync("DELETE FROM hero_section WHERE id = @Id", new { Id = id });
                return Ok(new { status = 200, message = "Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting hero section {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
