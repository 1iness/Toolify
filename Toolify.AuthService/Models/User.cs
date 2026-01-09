
namespace Toolify.AuthService.Models
{
    public class User 
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null;
        public string LastName { get; set; } = null;
        public string Email { get; set; } = null;
        public string Phone { get; set; } = null;
        public string Password { get; set; } = null;
        public string Role { get; set; } = "User";
        public bool EmailConfirmed { get; set; }
        public string? EmailConfirmCode { get; set; }
        public DateTime? EmailConfirmExpires { get; set; }
      
    }
}
