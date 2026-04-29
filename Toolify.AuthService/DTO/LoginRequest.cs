using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class LoginRequest 
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null;

        [Required]
        public string Password { get; set; } = null;
        public bool Remember { get; set; } = true;
    }
}
