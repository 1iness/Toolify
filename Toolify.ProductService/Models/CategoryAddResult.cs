namespace Toolify.ProductService.Models
{
    public class CategoryAddResult
    {
        public Category Category { get; set; } = null!;
        public bool AlreadyExists { get; set; }
    }
}
