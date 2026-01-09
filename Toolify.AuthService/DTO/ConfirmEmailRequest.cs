namespace Toolify.AuthService.DTO
{
    public class ConfirmEmailRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
