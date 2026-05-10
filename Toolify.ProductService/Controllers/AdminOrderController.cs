using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService;
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
            if (string.IsNullOrWhiteSpace(status)) return BadRequest("Статус не указан.");

            var current = await _repo.GetOrderStatusByIdAsync(id);
            if (string.IsNullOrWhiteSpace(current)) return NotFound();

            if (!OrderStatusWorkflow.TryNormalize(status, out var canonNext))
                return BadRequest("Недопустимое значение нового статуса.");

            if (!OrderStatusWorkflow.CanTransition(current, canonNext, out var err))
                return BadRequest(err);

            await _repo.UpdateOrderStatusAsync(id, canonNext);
            return Ok();
        }
    }
}