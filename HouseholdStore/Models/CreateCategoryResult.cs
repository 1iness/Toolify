using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class CreateCategoryResult
    {
        public bool IsSuccess { get; set; }
        public bool IsDuplicate { get; set; }
        public Category? Category { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
