using System.ComponentModel.DataAnnotations;

namespace Toolify.AuthService.DTO
{
    public class UpdateProfileRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
    }
}
