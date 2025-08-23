using System.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Extensions.Logging;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyHolderDetailController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<PolicyHolderDetailController> _logger;

        public PolicyHolderDetailController(IDbConnection db, ILogger<PolicyHolderDetailController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: All Policy Holders by AgentId
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetAll(int agentId)
        {
            try
            {
                var sql = @"SELECT PolicyHolderId, PolicyHolderName, ContactNumber, PolicyName, 
                                   AmountPerCycle, PaymentCycle, Status, CreatedDate, AgentId, Remarks
                            FROM policyholderdetail
                            WHERE AgentId = @AgentId";

                var result = await _db.QueryAsync(sql, new { AgentId = agentId });
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching policy holders for AgentId {AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        // GET: Single Policy Holder by Id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var sql = @"SELECT PolicyHolderId, PolicyHolderName, ContactNumber, PolicyName, 
                                   AmountPerCycle, PaymentCycle, Status, CreatedDate, AgentId, Remarks
                            FROM policyholderdetail
                            WHERE PolicyHolderId = @Id";

                var result = await _db.QueryFirstOrDefaultAsync(sql, new { Id = id });

                if (result == null)
                    return NotFound(new { status = 404, message = "Policy Holder not found" });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching policy holder with Id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        // POST: Create Policy Holder
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyHolderDetail model)
        {
            try
            {
                var sql = @"INSERT INTO policyholderdetail
                            (PolicyHolderName, ContactNumber, PolicyName, AmountPerCycle, PaymentCycle, Status, AgentId, Remarks)
                            VALUES (@PolicyHolderName, @ContactNumber, @PolicyName, @AmountPerCycle, @PaymentCycle, @Status, @AgentId, @Remarks)";

                await _db.ExecuteAsync(sql, model);
                return Ok(new { status = 200, message = "Policy Holder created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating policy holder");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        // PUT: Update Policy Holder
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PolicyHolderDetail model)
        {
            try
            {
                var sql = @"UPDATE policyholderdetail SET 
                                PolicyHolderName = @PolicyHolderName,
                                ContactNumber = @ContactNumber,
                                PolicyName = @PolicyName,
                                AmountPerCycle = @AmountPerCycle,
                                PaymentCycle = @PaymentCycle,
                                Status = @Status,
                                AgentId = @AgentId,
                                Remarks = @Remarks
                            WHERE PolicyHolderId = @Id";

                await _db.ExecuteAsync(sql, new
                {
                    model.PolicyHolderName,
                    model.ContactNumber,
                    model.PolicyName,
                    model.AmountPerCycle,
                    model.PaymentCycle,
                    model.Status,
                    model.AgentId,
                    model.Remarks,
                    Id = id
                });

                return Ok(new { status = 200, message = "Policy Holder updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating policy holder with Id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        // DELETE: Remove Policy Holder
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _db.ExecuteAsync(@"DELETE FROM policyholderdetail WHERE PolicyHolderId = @Id", new { Id = id });
                return Ok(new { status = 200, message = "Policy Holder deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting policy holder with Id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        // PATCH: Update Status Only
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            try
            {
                var sql = @"UPDATE policyholderdetail SET Status = @Status WHERE PolicyHolderId = @Id";
                await _db.ExecuteAsync(sql, new { Status = status, Id = id });

                return Ok(new { status = 200, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for PolicyHolderId {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
