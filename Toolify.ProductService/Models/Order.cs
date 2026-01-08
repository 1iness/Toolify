namespace Toolify.ProductService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string GuestFirstName { get; set; }
        public string GuestLastName { get; set; }
        public string GuestEmail { get; set; }
        public string GuestPhone { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "New";
        public string Address { get; set; } = string.Empty;
    }
}
