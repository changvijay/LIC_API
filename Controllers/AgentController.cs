using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly IDbConnection _db;
    public AgentController(IDbConnection db) => _db = db;


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Query agent by email
            var agent = await _db.QueryFirstOrDefaultAsync<Agent>(
                "SELECT id, name, password FROM agent_details WHERE email = @Email",
                new { Email = request.Email }
            );

            // Verify credentials
            if (agent == null || agent.Password != request.Password)
            {
                return Unauthorized(new { status = 401, message = "Invalid Credentials" });
            }

            // Return success response
            return Ok(new
            {
                status = 200,
                data = new
                {
                    id = agent.Id,
                    name = agent.Name
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = 500, error = ex.Message });
        }
    }
}

// Request model
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// Agent model
public class Agent
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Password { get; set; } // Only for verification
}