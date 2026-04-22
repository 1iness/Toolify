using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/discounts")]
    public class AdminDiscountController : ControllerBase
    {
        private readonly ProductRepository _repo;

        public AdminDiscountController(ProductRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _repo.GetDiscountsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DiscountWriteDto dto)
        {
            var err = Validate(dto);
            if (err != null) return BadRequest(err);

            var d = Build(dto);
            var id = await _repo.AddDiscountAsync(d);
            return Ok(new { id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DiscountWriteDto dto)
        {
            var err = Validate(dto);
            if (err != null) return BadRequest(err);

            var d = Build(dto);
            d.Id = id;
            await _repo.UpdateDiscountAsync(d);
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteDiscountAsync(id);
            return Ok();
        }

        private static Discount Build(DiscountWriteDto dto) => new()
        {
            Name = dto.Name?.Trim() ?? string.Empty,
            DiscountType = dto.DiscountType?.Trim() ?? string.Empty,
            ValueKind = dto.ValueKind?.Trim() ?? string.Empty,
            Value = dto.Value,
            CategoryId = (dto.DiscountType == DiscountTypes.Category
                          || (dto.DiscountType == DiscountTypes.Quantity && dto.CategoryId.HasValue))
                        ? dto.CategoryId : null,
            ProductId = (dto.DiscountType == DiscountTypes.Product
                         || (dto.DiscountType == DiscountTypes.Quantity && dto.ProductId.HasValue))
                        ? dto.ProductId : null,
            MinQuantity = dto.DiscountType == DiscountTypes.Quantity ? dto.MinQuantity : null,
            IsActive = dto.IsActive
        };

        private static string? Validate(DiscountWriteDto? dto)
        {
            if (dto == null) return "Пустое тело запроса";
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Укажите название скидки";

            var type = dto.DiscountType?.Trim();
            if (type != DiscountTypes.Quantity
                && type != DiscountTypes.Product
                && type != DiscountTypes.Category)
                return "Неверный тип скидки";

            var kind = dto.ValueKind?.Trim();
            if (kind != DiscountValueKinds.Percent && kind != DiscountValueKinds.Fixed)
                return "Неверный вид значения";

            if (kind == DiscountValueKinds.Percent && (dto.Value < 0 || dto.Value > 100))
                return "Процент должен быть от 0 до 100";
            if (kind == DiscountValueKinds.Fixed && dto.Value <= 0)
                return "Сумма должна быть больше 0";

            switch (type)
            {
                case DiscountTypes.Product:
                    if (!dto.ProductId.HasValue || dto.ProductId.Value < 1)
                        return "Укажите товар";
                    break;
                case DiscountTypes.Category:
                    if (!dto.CategoryId.HasValue || dto.CategoryId.Value < 1)
                        return "Укажите категорию";
                    break;
                case DiscountTypes.Quantity:
                    if (!dto.MinQuantity.HasValue || dto.MinQuantity.Value < 1)
                        return "Укажите минимальное количество";
                    if (!dto.ProductId.HasValue && !dto.CategoryId.HasValue)
                        return "Для скидки за количество укажите товар или категорию";
                    if (dto.ProductId.HasValue && dto.CategoryId.HasValue)
                        return "Выберите что-то одно: товар или категория";
                    break;
            }
            return null;
        }
    }

    public class DiscountWriteDto
    {
        public string? Name { get; set; }
        public string? DiscountType { get; set; }
        public string? ValueKind { get; set; }
        public decimal Value { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public int? MinQuantity { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
