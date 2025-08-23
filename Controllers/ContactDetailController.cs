using System.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactDetailController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<ContactDetailController> _logger;

        public ContactDetailController(IDbConnection db, ILogger<ContactDetailController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _db.QueryAsync<ContactDetail>(
                    @"SELECT id, 
                             name, 
                             phone_number AS PhoneNumber, 
                             email_id AS EmailId, 
                             service_required AS ServiceRequired, 
                             message_text AS MessageText, 
                             agent_id AS AgentId, 
                             status 
                      FROM contact_detail");

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all contact details");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _db.QueryFirstOrDefaultAsync<ContactDetail>(
                    @"SELECT id, 
                             name, 
                             phone_number AS PhoneNumber, 
                             email_id AS EmailId, 
                             service_required AS ServiceRequired, 
                             message_text AS MessageText, 
                             agent_id AS AgentId, 
                             status 
                      FROM contact_detail 
                      WHERE id = @Id", new { Id = id });

                if (result == null)
                {
                    return NotFound(new { status = 404, message = "Contact Detail Not Found" });
                }

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact detail by id={Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetByAgent(int agentId)
        {
            try
            {
                var result = await _db.QueryAsync<ContactDetail>(
                    @"SELECT id, 
                             name, 
                             phone_number AS PhoneNumber, 
                             email_id AS EmailId, 
                             service_required AS ServiceRequired, 
                             message_text AS MessageText, 
                             agent_id AS AgentId, 
                             status 
                      FROM contact_detail 
                      WHERE agent_id = @AgentId", new { AgentId = agentId });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact details for AgentId={AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(ContactDetail model)
        {
            try
            {
                var sql = @"INSERT INTO contact_detail 
                            (name, phone_number, email_id, service_required, message_text, agent_id, status) 
                            VALUES (@Name, @PhoneNumber, @EmailId, @ServiceRequired, @MessageText, @AgentId, 'Y')";

                await _db.ExecuteAsync(sql, model);

                return Ok(new { status = 200, message = "Contact Detail Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact detail");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id)
        {
            try
            {
                var sql = @"UPDATE contact_detail 
                            SET status = 'N' 
                            WHERE id = @Id";

                var rows = await _db.ExecuteAsync(sql, new { Id = id });

                if (rows == 0)
                {
                    return NotFound(new { status = 404, message = "Contact Detail Not Found" });
                }

                return Ok(new { status = 200, message = "Contact Detail Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact detail Id={Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var rows = await _db.ExecuteAsync("DELETE FROM contact_detail WHERE id = @Id", new { Id = id });

                if (rows == 0)
                {
                    return NotFound(new { status = 404, message = "Contact Detail Not Found" });
                }

                return Ok(new { status = 200, message = "Contact Detail Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact detail Id={Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
