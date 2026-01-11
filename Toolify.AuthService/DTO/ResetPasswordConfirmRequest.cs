namespace Toolify.AuthService.DTO
{
    public class ResetPasswordConfirmRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
