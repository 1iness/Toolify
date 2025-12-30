namespace Toolify.ProductService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "New";
        public string Address { get; set; } = string.Empty;
    }
}
