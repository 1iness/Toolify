using System.Net;
using System.Net.Mail;

namespace Toolify.AuthService.Services
{
    public class EmailService
    {
        public void SendConfirmCode(string toEmail, string code)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(
                    "",
                    ""
                ),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress("toolify.store@gmail.com", "Toolify Store"),
                Subject = "Подтверждение регистрации",
                Body = $"Ваш код подтверждения: {code}",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);
            smtp.Send(message);
     
        }

    }
}
