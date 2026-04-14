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

            if (model.MaxUses.HasValue && model.MaxUses.Value < 1)
                return BadRequest("Лимит использований должен быть не меньше 1");

            if (model.MinGoodsAmount.HasValue && model.MinGoodsAmount.Value < 0)
                return BadRequest("Минимальная сумма должна быть >= 0");

            await _repo.CreatePromoCodeAsync(
                model.Code,
                model.DiscountPercent,
                model.StartDate,
                model.EndDate,
                model.MaxUses,
                model.MinGoodsAmount);
            return Ok();
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> Validate(string code, [FromQuery] decimal? goodsTotal = null)
        {
            var allPromos = await _repo.GetAllPromoCodesAsync();
            var promo = allPromos.FirstOrDefault(p =>
                p.Code.Equals(code, StringComparison.OrdinalIgnoreCase) &&
                p.IsActive &&
                p.StartDate <= DateTime.Now &&
                 p.EndDate >= DateTime.Now &&
                (!p.MaxUses.HasValue || p.UsedCount < p.MaxUses.Value));

            if (promo == null) return NotFound("Промокод не найден, истёк или исчерпан");

            if (promo.MinGoodsAmount.HasValue && goodsTotal.HasValue && goodsTotal.Value < promo.MinGoodsAmount.Value)
                return NotFound($"Промокод действует от суммы {promo.MinGoodsAmount.Value:N2}");

            return Ok(new { discountPercent = promo.DiscountPercent });
        }
    }

    public class PromoCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxUses { get; set; }
        public decimal? MinGoodsAmount { get; set; }
    }
}