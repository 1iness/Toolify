using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrderController : ControllerBase
    {
        private readonly ProductRepository _repo;

        public AdminOrderController(ProductRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _repo.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (string.IsNullOrEmpty(status)) return BadRequest();

            await _repo.UpdateOrderStatusAsync(id, status);
            return Ok();
        }
    }
}