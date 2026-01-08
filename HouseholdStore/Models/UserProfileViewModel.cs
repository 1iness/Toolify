using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class UserProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<OrderHistoryDto> Orders { get; set; } = new();
    }
}
