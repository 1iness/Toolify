using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class DashboardViewModel
    {
        public List<CategoryProductReport> ProductsByCategory { get; set; } = new();
        public List<OrderStatusReport> OrdersByStatus { get; set; } = new();
        public List<ClientHistoryReport> ClientHistory { get; set; } = new();
    }
}