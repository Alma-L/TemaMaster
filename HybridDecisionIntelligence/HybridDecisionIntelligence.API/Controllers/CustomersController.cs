using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HybridDecisionIntelligence.API.Controllers
{
    /// <summary>
    /// Read-only endpoint exposing the customer profile data submitted with
    /// a decision request, so the frontend can show who a decision was
    /// actually about, not just the outcome.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly IBankCustomerRepository _customerRepository;

        public CustomersController(IBankCustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Get a customer's stored profile by Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerRepository.FindCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"No stored profile for customer {id}" });
            }

            return Ok(customer);
        }
    }
}
