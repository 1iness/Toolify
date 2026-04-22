using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/promotions")]
    public class AdminPromotionController : ControllerBase
    {
        private readonly ProductRepository _repo;

        public AdminPromotionController(ProductRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _repo.GetPromotionsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromotionWriteDto dto)
        {
            var err = Validate(dto, isUpdate: false);
            if (err != null) return BadRequest(err);

            var p = Build(dto);
            var id = await _repo.AddPromotionAsync(p);
            return Ok(new { id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PromotionWriteDto dto)
        {
            var err = Validate(dto, isUpdate: true);
            if (err != null) return BadRequest(err);

            var p = Build(dto);
            p.Id = id;
            await _repo.UpdatePromotionAsync(p);
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeletePromotionAsync(id);
            return Ok();
        }

        [HttpGet("product-status/{productId:int}")]
        public async Task<IActionResult> GetProductStatus(int productId)
        {
            if (productId < 1) return BadRequest("Некорректный productId");
            return Ok(await _repo.GetDiscountStatusForProductAsync(productId));
        }

        private static Promotion Build(PromotionWriteDto dto) => new()
        {
            Name = dto.Name?.Trim() ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description!.Trim(),
            PromotionType = dto.PromotionType?.Trim() ?? string.Empty,
            ScopeType = dto.ScopeType?.Trim() ?? string.Empty,
            CategoryId = dto.ScopeType == PromotionScopes.Category ? dto.CategoryId : null,
            ProductId = dto.ScopeType == PromotionScopes.Product ? dto.ProductId : null,
            BuyQty = dto.PromotionType == PromotionTypes.BuyGetY ? dto.BuyQty : null,
            PayQty = dto.PromotionType == PromotionTypes.BuyGetY ? dto.PayQty : null,
            PercentOff = dto.PromotionType == PromotionTypes.OrderPercent ? dto.PercentOff : null,
            MinOrderAmount = dto.MinOrderAmount,
            GiftDescription = dto.PromotionType == PromotionTypes.Gift ? dto.GiftDescription?.Trim() : null,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            IsActive = dto.IsActive
        };

        private static string? Validate(PromotionWriteDto? dto, bool isUpdate)
        {
            if (dto == null) return "Пустое тело запроса";
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Укажите название акции";
            if (dto.EndDate < dto.StartDate) return "Дата окончания раньше даты начала";

            var type = dto.PromotionType?.Trim();
            if (type != PromotionTypes.BuyGetY
                && type != PromotionTypes.OrderPercent
                && type != PromotionTypes.FreeShipping
                && type != PromotionTypes.Gift)
                return "Неверный тип акции";

            var scope = dto.ScopeType?.Trim();
            if (scope != PromotionScopes.All
                && scope != PromotionScopes.Category
                && scope != PromotionScopes.Product)
                return "Неверная область действия";

            if (scope == PromotionScopes.Category && (!dto.CategoryId.HasValue || dto.CategoryId.Value < 1))
                return "Для категории укажите CategoryId";
            if (scope == PromotionScopes.Product && (!dto.ProductId.HasValue || dto.ProductId.Value < 1))
                return "Для товара укажите ProductId";

            switch (type)
            {
                case PromotionTypes.BuyGetY:
                    if (scope == PromotionScopes.All) return "«Купи-получи» требует категорию или товар";
                    if (!dto.BuyQty.HasValue || !dto.PayQty.HasValue)
                        return "Укажите параметры «купить» и «оплатить»";
                    if (dto.BuyQty.Value < 2) return "«Купить» должно быть >= 2";
                    if (dto.PayQty.Value < 1) return "«Оплатить» должно быть >= 1";
                    if (dto.PayQty.Value >= dto.BuyQty.Value)
                        return "«Оплатить» должно быть меньше «купить»";
                    break;
                case PromotionTypes.OrderPercent:
                    if (!dto.PercentOff.HasValue) return "Укажите процент";
                    if (dto.PercentOff.Value < 0 || dto.PercentOff.Value > 100)
                        return "Процент должен быть от 0 до 100";
                    break;
                case PromotionTypes.Gift:
                    if (string.IsNullOrWhiteSpace(dto.GiftDescription))
                        return "Укажите описание подарка";
                    break;
            }

            if (dto.MinOrderAmount.HasValue && dto.MinOrderAmount.Value < 0)
                return "Минимальная сумма не может быть отрицательной";

            return null;
        }
    }

    public class PromotionWriteDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? PromotionType { get; set; }
        public string? ScopeType { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public int? BuyQty { get; set; }
        public int? PayQty { get; set; }
        public decimal? PercentOff { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public string? GiftDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
