using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class ConfirmEmailRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(32)]
        public string Code { get; set; } = null!;
    }
}
