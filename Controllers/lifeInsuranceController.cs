using System.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LifeInsuranceController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<LifeInsuranceController> _logger;

        public LifeInsuranceController(IDbConnection db, ILogger<LifeInsuranceController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllWithDetails()
        {
            try
            {
                var sql = @"SELECT id, title AS Title, subtitle AS Subtitle, description AS Description, 
                                   icon_name AS IconName, age_range AS AgeRange, 
                                   min_premium AS MinPremium, popular AS Popular, agent_id AS AgentId 
                            FROM life_insurance_product";
                var products = (await _db.QueryAsync(sql)).ToList();

                foreach (var product in products)
                {
                    var productId = (int)product.id;

                    var features = await _db.QueryAsync<string>(
                        "SELECT feature_text FROM life_insurance_feature WHERE product_id = @ProductId",
                        new { ProductId = productId });

                    var plans = await _db.QueryAsync<string>(
                        "SELECT plan_name FROM life_insurance_plan WHERE product_id = @ProductId",
                        new { ProductId = productId });

                    product.Features = features.ToList();
                    product.Plans = plans.ToList();
                }

                return Ok(new { status = 200, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all life insurance products");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var product = await _db.QueryFirstOrDefaultAsync("SELECT * FROM life_insurance_product WHERE id = @Id", new { Id = id });
                if (product == null) return NotFound(new { status = 404, message = "Not Found" });

                var features = await _db.QueryAsync("SELECT feature_text FROM life_insurance_feature WHERE product_id = @Id", new { Id = id });
                var plans = await _db.QueryAsync("SELECT plan_name FROM life_insurance_plan WHERE product_id = @Id", new { Id = id });

                return Ok(new
                {
                    status = 200,
                    data = new
                    {
                        product,
                        features = features.Select(f => f.feature_text).ToList(),
                        plans = plans.Select(p => p.plan_name).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching life insurance product {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(LifeInsuranceProductRequest model)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                using var transaction = _db.BeginTransaction();

                // MySQL replacement for RETURNING id
                var productId = await _db.ExecuteScalarAsync<long>(@"
                    INSERT INTO life_insurance_product 
                        (title, subtitle, description, icon_name, age_range, min_premium, popular, agent_id) 
                    VALUES (@Title, @Subtitle, @Description, @IconName, @AgeRange, @MinPremium, @Popular, @AgentId);
                    SELECT LAST_INSERT_ID();", model, transaction);

                foreach (var feature in model.Features)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO life_insurance_feature (product_id, feature_text) VALUES (@ProductId, @FeatureText)",
                        new { ProductId = productId, FeatureText = feature }, transaction);
                }

                foreach (var plan in model.Plans)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO life_insurance_plan (product_id, plan_name) VALUES (@ProductId, @PlanName)",
                        new { ProductId = productId, PlanName = plan }, transaction);
                }

                transaction.Commit();
                return Ok(new { status = 200, message = "Created Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating life insurance product");
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LifeInsuranceProductRequest model)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();
                using var transaction = _db.BeginTransaction();

                // --- Update main product ---
                await _db.ExecuteAsync(@"
        UPDATE life_insurance_product SET 
            title = @Title, subtitle = @Subtitle, description = @Description, 
            icon_name = @IconName, age_range = @AgeRange, 
            min_premium = @MinPremium, popular = @Popular, agent_id = @AgentId 
        WHERE id = @Id",
                    new
                    {
                        model.Title,
                        model.Subtitle,
                        model.Description,
                        model.IconName,
                        model.AgeRange,
                        model.MinPremium,
                        model.Popular,
                        model.AgentId,
                        Id = id
                    }, transaction);


                // --- Sync Features ---
                var existingFeatures = (await _db.QueryAsync<string>(
                    "SELECT feature_text FROM life_insurance_feature WHERE product_id = @Id",
                    new { Id = id }, transaction)).ToList();

                var featuresToAdd = model.Features.Except(existingFeatures).ToList();
                var featuresToRemove = existingFeatures.Except(model.Features).ToList();

                foreach (var feature in featuresToAdd)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO life_insurance_feature (product_id, feature_text) VALUES (@ProductId, @FeatureText)",
                        new { ProductId = id, FeatureText = feature }, transaction);
                }

                foreach (var feature in featuresToRemove)
                {
                    await _db.ExecuteAsync(
                        "DELETE FROM life_insurance_feature WHERE product_id = @ProductId AND feature_text = @FeatureText",
                        new { ProductId = id, FeatureText = feature }, transaction);
                }


                // --- Sync Plans ---
                var existingPlans = (await _db.QueryAsync<string>(
                    "SELECT plan_name FROM life_insurance_plan WHERE product_id = @Id",
                    new { Id = id }, transaction)).ToList();

                var plansToAdd = model.Plans.Except(existingPlans).ToList();
                var plansToRemove = existingPlans.Except(model.Plans).ToList();

                foreach (var plan in plansToAdd)
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO life_insurance_plan (product_id, plan_name) VALUES (@ProductId, @PlanName)",
                        new { ProductId = id, PlanName = plan }, transaction);
                }

                foreach (var plan in plansToRemove)
                {
                    await _db.ExecuteAsync(
                        "DELETE FROM life_insurance_plan WHERE product_id = @ProductId AND plan_name = @PlanName",
                        new { ProductId = id, PlanName = plan }, transaction);
                    await _db.ExecuteAsync(
                        "DELETE FROM plan_details WHERE NAME = @PlanName and agent_id = @AgentId",
                        new { AgentId = model.AgentId, PlanName = plan }, transaction);

                }


                transaction.Commit();
                return Ok(new { status = 200, message = "Updated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating life insurance product {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = _db.BeginTransaction();

                await _db.ExecuteAsync("DELETE FROM life_insurance_feature WHERE product_id = @Id", new { Id = id }, transaction);
                await _db.ExecuteAsync("DELETE FROM life_insurance_plan WHERE product_id = @Id", new { Id = id }, transaction);
                await _db.ExecuteAsync("DELETE FROM life_insurance_product WHERE id = @Id", new { Id = id }, transaction);

                transaction.Commit();
                return Ok(new { status = 200, message = "Deleted Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting life insurance product {Id}", id);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }

        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetAllByAgent(int agentId)
        {
            try
            {
                var sql = @"SELECT id, title AS Title, subtitle AS Subtitle, description AS Description, 
                                   icon_name AS IconName, age_range AS AgeRange, 
                                   min_premium AS MinPremium, popular AS Popular, agent_id AS AgentId 
                            FROM life_insurance_product  
                            WHERE agent_id = @AgentId";

                var products = (await _db.QueryAsync(sql, new { AgentId = agentId })).ToList();

                foreach (var product in products)
                {
                    var productId = (int)product.id;

                    var features = await _db.QueryAsync<string>(
                        "SELECT feature_text FROM life_insurance_feature WHERE product_id = @ProductId",
                        new { ProductId = productId });

                    var plans = await _db.QueryAsync<string>(
                        "SELECT plan_name FROM life_insurance_plan WHERE product_id = @ProductId",
                        new { ProductId = productId });

                    product.Features = features.ToList();
                    product.Plans = plans.ToList();
                }

                return Ok(new { status = 200, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching life insurance products for agent {AgentId}", agentId);
                return StatusCode(500, new { status = 500, error = ex.Message });
            }
        }
    }
}
