namespace Toolify.ProductService.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public int? MaxUses { get; set; }

        public int UsedCount { get; set; }

        public bool IsValid =>
            IsActive
            && DateTime.Now >= StartDate
            && DateTime.Now <= EndDate
            && (!MaxUses.HasValue || UsedCount < MaxUses.Value);
    }
}