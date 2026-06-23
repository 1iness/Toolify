using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Helpers;
using Toolify.ProductService.Models;

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

        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromBody] SetPromoCodeActiveDto model)
        {
            if (id <= 0) return BadRequest("Некорректный идентификатор промокода");

            var ok = await _repo.SetPromoCodeActiveAsync(id, model.IsActive);
            return ok ? Ok() : NotFound("Промокод не найден");
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest("Некорректный идентификатор промокода");

            try
            {
                var ok = await _repo.DeletePromoCodeAsync(id);
                return ok ? Ok() : NotFound("Промокод не найден");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> Validate(string code, [FromQuery] decimal? goodsTotal = null)
        {
            var promo = await _repo.GetPromoCodeByCodeAsync(code);
            var rejectReason = PromoCodeValidation.GetRejectReason(promo, code, goodsTotal);

            if (rejectReason != null)
                return NotFound(rejectReason);

            return Ok(new { discountPercent = promo!.DiscountPercent });
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

    public class SetPromoCodeActiveDto
    {
        public bool IsActive { get; set; }
    }
}