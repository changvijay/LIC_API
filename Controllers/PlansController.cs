using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<PlansController> _logger;

        public PlansController(IDbConnection db, ILogger<PlansController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetAll(int agentId)
        {
            try
            {
                var sql = $@"SELECT 
                            id,
                            name AS Name,
                            description AS Description,
                            category AS Category,
                            min_entry_age AS MinEntryAge,
                            max_entry_age AS MaxEntryAge,
                            maturity_age AS MaturityAge,
                            min_sum_assured AS MinSumAssured,
                            max_sum_assured AS MaxSumAssured,
                            premium_payment_term AS PremiumPaymentTerm,
                            policy_term AS PolicyTerm,
                            features AS Features,
                            benefits AS Benefits,
                            eligibility AS Eligibility,
                            documents_required AS DocumentsRequired,
                            popularity AS Popularity,
                            claim_settlement_ratio AS ClaimSettlementRatio
                        FROM plan_details where agent_id = {agentId} ";

                var raw = await _db.QueryAsync<PlanDetailRow>(sql);

                var result = raw.Select(r => new PlanDetail
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Category = r.Category,
                    MinEntryAge = r.MinEntryAge,
                    MaxEntryAge = r.MaxEntryAge,
                    MaturityAge = r.MaturityAge,
                    MinSumAssured = r.MinSumAssured,
                    MaxSumAssured = r.MaxSumAssured,
                    PremiumPaymentTerm = r.PremiumPaymentTerm,
                    PolicyTerm = r.PolicyTerm,
                    Features = string.IsNullOrEmpty(r.Features)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(r.Features),
                    Benefits = string.IsNullOrEmpty(r.Benefits)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(r.Benefits),
                    Eligibility = r.Eligibility,
                    DocumentsRequired = string.IsNullOrEmpty(r.DocumentsRequired)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(r.DocumentsRequired),
                    Popularity = r.Popularity,
                    ClaimSettlementRatio = r.ClaimSettlementRatio
                }).ToList();



                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all plans");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("byname/{PlanName}")]
        public async Task<IActionResult> GetAllbyplansname(string PlanName)
        {
            try
            {
                var sql = $@"SELECT 
                            id,
                            name AS Name,
                            description AS Description,
                            category AS Category,
                            min_entry_age AS MinEntryAge,
                            max_entry_age AS MaxEntryAge,
                            maturity_age AS MaturityAge,
                            min_sum_assured AS MinSumAssured,
                            max_sum_assured AS MaxSumAssured,
                            premium_payment_term AS PremiumPaymentTerm,
                            policy_term AS PolicyTerm,
                            features AS Features,
                            benefits AS Benefits,
                            eligibility AS Eligibility,
                            documents_required AS DocumentsRequired,
                            popularity AS Popularity,
                            claim_settlement_ratio AS ClaimSettlementRatio
                        FROM plan_details where Name = '{PlanName}' ";

                var result = await _db.QueryAsync<PlanDetailRow>(sql);
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all plans");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("RelatedPlans/{PlanName}")]
        public async Task<IActionResult> RelatedPlans(string PlanName)
        {
            try
            {
                var sql = $@"SELECT id, NAME, category,DESCRIPTION FROM `plan_details` WHERE category = '{PlanName}'";

                var result = await _db.QueryAsync<RelatedPlans>(sql);
                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all plans");
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
                            name AS Name,
                            description AS Description,
                            category AS Category,
                            min_entry_age AS MinEntryAge,
                            max_entry_age AS MaxEntryAge,
                            maturity_age AS MaturityAge,
                            min_sum_assured AS MinSumAssured,
                            max_sum_assured AS MaxSumAssured,
                            premium_payment_term AS PremiumPaymentTerm,
                            policy_term AS PolicyTerm,
                            features AS Features,
                            benefits AS Benefits,
                            eligibility AS Eligibility,
                            documents_required AS DocumentsRequired,
                            popularity AS Popularity,
                            claim_settlement_ratio AS ClaimSettlementRatio
                        FROM plan_details 
                        WHERE id = @Id";

                var result = await _db.QueryFirstOrDefaultAsync<PlanDetail>(sql, new { Id = id });

                if (result == null)
                    return NotFound(new { status = 404, message = "Plan Not Found" });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plan by id {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(PlanDetail model)
        {
            try
            {
                var sql = @"INSERT INTO plan_details 
                            (name, description, category, min_entry_age, max_entry_age, 
                             maturity_age, min_sum_assured, max_sum_assured, premium_payment_term, 
                             policy_term, features, benefits, eligibility, documents_required, 
                             popularity, claim_settlement_ratio, agent_id) 
                            VALUES (@Name, @Description, @Category, @MinEntryAge, @MaxEntryAge, 
                                    @MaturityAge, @MinSumAssured, @MaxSumAssured, @PremiumPaymentTerm, 
                                    @PolicyTerm, @Features, @Benefits, @Eligibility, @DocumentsRequired, 
                                    @Popularity, @ClaimSettlementRatio, @AgentId)";

                var parameters = new
                {
                    model.Name,
                    model.Description,
                    model.Category,
                    model.MinEntryAge,
                    model.MaxEntryAge,
                    model.MaturityAge,
                    model.MinSumAssured,
                    model.MaxSumAssured,
                    model.PremiumPaymentTerm,
                    model.PolicyTerm,
                    Features = JsonConvert.SerializeObject(model.Features),
                    Benefits = JsonConvert.SerializeObject(model.Benefits),
                    model.Eligibility,
                    DocumentsRequired = JsonConvert.SerializeObject(model.DocumentsRequired),
                    model.Popularity,
                    model.ClaimSettlementRatio,
                    model.AgentId
                };

                await _db.ExecuteAsync(sql, parameters);
                return Ok(new { status = 200, message = "Plan Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating plan");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PlanDetail model)
        {
            try
            {
                var sql = @"UPDATE plan_details SET 
                            name = @Name,
                            description = @Description,
                            category = @Category,
                            min_entry_age = @MinEntryAge,
                            max_entry_age = @MaxEntryAge,
                            maturity_age = @MaturityAge,
                            min_sum_assured = @MinSumAssured,
                            max_sum_assured = @MaxSumAssured,
                            premium_payment_term = @PremiumPaymentTerm,
                            policy_term = @PolicyTerm,
                            features = @Features,
                            benefits = @Benefits,
                            eligibility = @Eligibility,
                            documents_required = @DocumentsRequired,
                            popularity = @Popularity,
                            claim_settlement_ratio = @ClaimSettlementRatio
                        WHERE id = @Id";

                var parameters = new
                {
                    model.Name,
                    model.Description,
                    model.Category,
                    model.MinEntryAge,
                    model.MaxEntryAge,
                    model.MaturityAge,
                    model.MinSumAssured,
                    model.MaxSumAssured,
                    model.PremiumPaymentTerm,
                    model.PolicyTerm,
                    Features = JsonConvert.SerializeObject(model.Features),
                    Benefits = JsonConvert.SerializeObject(model.Benefits),
                    model.Eligibility,
                    DocumentsRequired = JsonConvert.SerializeObject(model.DocumentsRequired),
                    model.Popularity,
                    model.ClaimSettlementRatio,
                    Id = id
                };

                await _db.ExecuteAsync(sql, parameters);
                return Ok(new { status = 200, message = "Plan Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating plan {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _db.ExecuteAsync("DELETE FROM plan_details WHERE id = @Id", new { Id = id });
                return Ok(new { status = 200, message = "Plan Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting plan {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("planList/{agentId}")]
        public async Task<IActionResult> planList(int agentId)
        {
            try
            {
                var sql = $@"SELECT 
                            p.title,
                            JSON_ARRAYAGG(pl.plan_name) AS plan_Name
                        FROM life_insurance_product p
                        LEFT JOIN life_insurance_plan pl 
                                   ON pl.product_id = p.id
                            LEFT JOIN plan_details d
                                   ON d.name = pl.plan_name 
                                  AND d.agent_id = p.agent_id
                            WHERE p.agent_id = @agentId
                                  AND d.name IS NULL
                                GROUP BY p.title;
                ";

                var result = await _db.QueryAsync<PlanList>(sql, new { agentId = agentId });

                if (result == null)
                    return NotFound(new { status = 404, message = "Plan Not Found" });

                return Ok(new { status = 200, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plan by id {Id}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}