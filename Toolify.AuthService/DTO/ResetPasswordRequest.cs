using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;

        [Required]
        public string NewPassword { get; set; } = null!;
    }
}
