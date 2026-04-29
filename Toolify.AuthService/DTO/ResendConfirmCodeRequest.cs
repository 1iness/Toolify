using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class ResendConfirmCodeRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;
    }
}
