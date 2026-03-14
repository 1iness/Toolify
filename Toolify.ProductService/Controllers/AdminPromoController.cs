using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Models;
using Toolify.ProductService.Data;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/promocodes")] 
    public class AdminPromoController : ControllerBase
    {
        private readonly ProductRepository _repo;

        public AdminPromoController(ProductRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var promos = await _repo.GetAllPromoCodesAsync();
            return Ok(promos);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromoCodeDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Code))
                return BadRequest("Некорректные данные промокода");

            await _repo.CreatePromoCodeAsync(model.Code, model.DiscountPercent, model.StartDate, model.EndDate);
            return Ok();
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> Validate(string code)
        {
            var allPromos = await _repo.GetAllPromoCodesAsync();
            var promo = allPromos.FirstOrDefault(p =>
                p.Code.Equals(code, StringComparison.OrdinalIgnoreCase) &&
                p.IsActive &&
                p.StartDate <= DateTime.Now &&
                p.EndDate >= DateTime.Now);

            if (promo == null) return NotFound("Промокод не найден или истек");

            return Ok(new { discountPercent = promo.DiscountPercent });
        }
    }

    public class PromoCodeDto
    {
        public string Code { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}