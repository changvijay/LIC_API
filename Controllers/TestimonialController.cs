using System.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestimonialController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<TestimonialController> _logger;

        public TestimonialController(IDbConnection db, ILogger<TestimonialController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _db.QueryAsync<Testimonial>(
                    "SELECT * FROM testimonial ORDER BY id ASC");
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all testimonials");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _db.QueryFirstOrDefaultAsync<Testimonial>(
                    "SELECT * FROM testimonial WHERE id = @Id",
                    new { Id = id });

                if (result == null)
                    return NotFound(new { status = 404, message = "Testimonial Not Found" });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching testimonial by Id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetByAgent(int agentId)
        {
            try
            {
                var result = await _db.QueryAsync<Testimonial>(
                    "SELECT * FROM testimonial WHERE agent_id = @AgentId AND status = 'Y'",
                    new { AgentId = agentId });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching testimonials for agent {AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(Testimonial model)
        {
            try
            {
                var sql = @"INSERT INTO testimonial 
                            (name, location, testimonialtext, rating, agent_id, status) 
                            VALUES (@Name, @Location, @TestimonialText, @Rating, @AgentId, 'Y')";

                await _db.ExecuteAsync(sql, model);
                return Ok(new { status = 200, message = "Testimonial Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating testimonial");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id)
        {
            try
            {
                var sql = @"UPDATE testimonial SET status = 'N' WHERE id = @Id";
                var rows = await _db.ExecuteAsync(sql, new { Id = id });

                if (rows == 0)
                    return NotFound(new { status = 404, message = "Testimonial Not Found" });

                return Ok(new { status = 200, message = "Testimonial Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating testimonial {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var rows = await _db.ExecuteAsync(
                    "DELETE FROM testimonial WHERE id = @Id", new { Id = id });

                if (rows == 0)
                    return NotFound(new { status = 404, message = "Testimonial Not Found" });

                return Ok(new { status = 200, message = "Testimonial Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting testimonial {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
