using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class RegisterRequest 
    {
        public string FirstName { get; set; } = null;
        public string LastName { get; set; } = null;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null;
        public string Phone { get; set; } = null;
        public string Password { get; set; } = null;
        public string ConfirmPassword { get; set; } = null;
    }
}
