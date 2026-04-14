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

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            return Ok(await _repo.GetDiscountCampaignsAsync());
        }

        [HttpPost("campaigns")]
        public async Task<IActionResult> AddCampaign([FromBody] DiscountCampaignWriteDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Укажите название акции");
            if (dto.EndDate < dto.StartDate)
                return BadRequest("Дата окончания раньше даты начала");

            var id = await _repo.AddDiscountCampaignAsync(
                dto.Name.Trim(),
                string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                dto.StartDate,
                dto.EndDate,
                dto.IsActive,
                dto.Priority);
            return Ok(new { id });
        }

        [HttpPut("campaigns/{id:int}")]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] DiscountCampaignWriteDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Укажите название акции");
            if (dto.EndDate < dto.StartDate)
                return BadRequest("Дата окончания раньше даты начала");

            await _repo.UpdateDiscountCampaignAsync(
                id,
                dto.Name.Trim(),
                string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                dto.StartDate,
                dto.EndDate,
                dto.IsActive,
                dto.Priority);
            return Ok();
        }

        [HttpDelete("campaigns/{id:int}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            await _repo.DeleteDiscountCampaignAsync(id);
            return Ok();
        }

        [HttpGet("rules")]
        public async Task<IActionResult> GetRules()
        {
            return Ok(await _repo.GetDiscountRulesAsync());
        }

        [HttpPost("rules")]
        public async Task<IActionResult> AddRule([FromBody] DiscountRuleWriteDto dto)
        {
            var err = ValidateRuleDto(dto);
            if (err != null) return BadRequest(err);

            var id = await _repo.AddDiscountRuleAsync(
                dto.CampaignId,
                dto.ScopeType!.Trim(),
                dto.ScopeType == "Category" ? dto.CategoryId : null,
                dto.ScopeType == "Client" ? dto.UserId : null,
                dto.DiscountMode!.Trim(),
                dto.DiscountValue,
                dto.MinGoodsAmount,
                dto.IsActive);
            return Ok(new { id });
        }

        [HttpPut("rules/{id:int}")]
        public async Task<IActionResult> UpdateRule(int id, [FromBody] DiscountRuleWriteDto dto)
        {
            var err = ValidateRuleDto(dto);
            if (err != null) return BadRequest(err);

            await _repo.UpdateDiscountRuleAsync(
                id,
                dto.CampaignId,
                dto.ScopeType!.Trim(),
                dto.ScopeType == "Category" ? dto.CategoryId : null,
                dto.ScopeType == "Client" ? dto.UserId : null,
                dto.DiscountMode!.Trim(),
                dto.DiscountValue,
                dto.MinGoodsAmount,
                dto.IsActive);
            return Ok();
        }

        [HttpDelete("rules/{id:int}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            await _repo.DeleteDiscountRuleAsync(id);
            return Ok();
        }

        private static string? ValidateRuleDto(DiscountRuleWriteDto? dto)
        {
            if (dto == null) return "Пустое тело запроса";
            var scope = dto.ScopeType?.Trim();
            if (scope != "Category" && scope != "Client")
                return "ScopeType должен быть Category или Client";
            if (scope == "Category" && (!dto.CategoryId.HasValue || dto.CategoryId.Value < 1))
                return "Для скидки по категории укажите CategoryId";
            if (scope == "Client" && (!dto.UserId.HasValue || dto.UserId.Value < 1))
                return "Для персональной скидки укажите UserId";
            var mode = dto.DiscountMode?.Trim();
            if (mode != "Percent" && mode != "Amount")
                return "DiscountMode должен быть Percent или Amount";
            if (mode == "Percent" && (dto.DiscountValue < 0 || dto.DiscountValue > 100))
                return "Процент должен быть от 0 до 100";
            if (mode == "Amount" && dto.DiscountValue <= 0)
                return "Фиксированная скидка должна быть больше 0";
            if (dto.MinGoodsAmount.HasValue && dto.MinGoodsAmount.Value < 0)
                return "Минимальная сумма заказа не может быть отрицательной";
            return null;
        }
    }

    public class DiscountCampaignWriteDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; }
    }

    public class DiscountRuleWriteDto
    {
        public int? CampaignId { get; set; }
        public string? ScopeType { get; set; }
        public int? CategoryId { get; set; }
        public int? UserId { get; set; }
        public string? DiscountMode { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinGoodsAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
