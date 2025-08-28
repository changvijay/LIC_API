using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Extensions.Logging;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<BlogController> _logger;

        public BlogController(IDbConnection db, ILogger<BlogController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var sql = @"
                    SELECT 
                        id,
                        sorting_order AS SortingOrder,
                        title AS Title,
                        excerpt AS Excerpt,
                        image_url AS ImageUrl,
                        category AS Category,
                        author AS Author,
                        published_date AS PublishedDate,
                        agent_id AS AgentId,
                        SrcUrl
                    FROM blog
                    ORDER BY sorting_order ASC;";

                var result = await _db.QueryAsync<Blog>(sql);
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all blogs");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var sql = @"SELECT 
                                id, 
                                sorting_order AS SortingOrder, 
                                title AS Title, 
                                excerpt AS Excerpt, 
                                image_url AS ImageUrl, 
                                category AS Category, 
                                author AS Author,
                                published_date AS PublishedDate, 
                                agent_id AS AgentId,
                                SrcUrl
                            FROM blog 
                            WHERE id = @Id";

                var result = await _db.QueryFirstOrDefaultAsync<Blog>(sql, new { Id = id });

                if (result == null)
                    return NotFound(new { status = 404, message = "Blog Not Found" });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blog by id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetByAgent(int agentId)
        {
            try
            {
                var sql = @"SELECT 
                                id, 
                                sorting_order AS SortingOrder, 
                                title AS Title, 
                                excerpt AS Excerpt, 
                                image_url AS ImageUrl, 
                                category AS Category, 
                                author AS Author,
                                published_date AS PublishedDate, 
                                agent_id AS AgentId,
                                SrcUrl
                            FROM blog 
                            WHERE agent_id = @AgentId 
                            ORDER BY sorting_order ASC";

                var result = await _db.QueryAsync<Blog>(sql, new { AgentId = agentId });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blogs for agent {AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(Blog model)
        {
            try
            {
                var sql = @"INSERT INTO blog 
                            (sorting_order, title, excerpt, image_url, category, author, published_date, agent_id, SrcUrl) 
                            VALUES (@SortingOrder, @Title, @Excerpt, @ImageUrl, @Category, @Author, @PublishedDate, @AgentId, @SrcUrl)";

                await _db.ExecuteAsync(sql, model);
                return Ok(new { status = 200, message = "Blog Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Blog model)
        {
            try
            {
                var sql = @"UPDATE blog SET 
                                sorting_order = @SortingOrder, 
                                title = @Title, 
                                excerpt = @Excerpt, 
                                image_url = @ImageUrl, 
                                category = @Category, 
                                author = @Author, 
                                published_date = @PublishedDate, 
                                agent_id = @AgentId,
                                SrcUrl = @SrcUrl
                            WHERE id = @Id";

                await _db.ExecuteAsync(sql, new
                {
                    model.SortingOrder,
                    model.Title,
                    model.Excerpt,
                    model.ImageUrl,
                    model.Category,
                    model.Author,
                    model.PublishedDate,
                    model.AgentId,
                    model.SrcUrl,
                    Id = id
                });

                return Ok(new { status = 200, message = "Blog Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _db.ExecuteAsync("DELETE FROM blog WHERE id = @Id", new { Id = id });
                return Ok(new { status = 200, message = "Blog Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
