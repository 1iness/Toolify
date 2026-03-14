namespace Toolify.ProductService.Models
{
    public class CategoryProductReport
    {
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }
    public class OrderStatusReport
    {
        public string Status { get; set; }
        public int OrderCount { get; set; } 
    }

    public class ClientHistoryReport
    {
        public string ClientName { get; set; }  
        public string ClientEmail { get; set; } 
        public int TotalOrders { get; set; }    
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
    }
}