using System.Net;
using System.Net.Mail;

namespace Toolify.AuthService.Services
{
    public class EmailService
    {
        private readonly string _email = "";
        private readonly string _password = "";

        private SmtpClient CreateClient()
        {
            return new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_email, _password),
                EnableSsl = true
            };
        }

        // сообщение для подтверждения регистрации
        public void SendRegistrationCode(string toEmail, string code)
        {
            var smtp = CreateClient();

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = "Подтверждение регистрации",
                Body = $"Ваш код подтверждения регистрации:\n\n{code}\n\nЕсли вы не регистрировались — просто проигнорируйте это письмо.",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);
            smtp.Send(message);
        }

        // сообщения для восстановления пароля
        public void SendResetPasswordCode(string toEmail, string code)
        {
            var smtp = CreateClient();

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = "Восстановление пароля",
                Body = $"Вы запросили восстановление пароля.\n\nВаш код:\n{code}\n\nЕсли это были не вы — просто проигнорируйте письмо.",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);
            smtp.Send(message);
        }
    }
}

