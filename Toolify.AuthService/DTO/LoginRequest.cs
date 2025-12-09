
namespace Toolify.AuthService.DTO
{
    public class LoginRequest 
    {
        public string Email { get; set; } = null;
        public string Password { get; set; } = null;
        public bool Remember { get; set; } = true;
    }
}
