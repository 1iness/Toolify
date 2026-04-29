using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;
    }
}
