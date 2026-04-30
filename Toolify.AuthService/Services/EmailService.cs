using System.Net;
using System.Net.Mail;
using System.Text;

namespace Toolify.AuthService.Services
{
    public class EmailService
    {
        private readonly string _email = "toolifyhousestore@gmail.com";
        private readonly string _password = "tbjdzutbhcuibfmy";

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

        //сообщени на почту при оформлении заказа 
        public async Task SendOrderConfirmedAsync(string toEmail, int orderId)
        {
            var smtp = CreateClient();

            var message = new MailMessage
            {
                From = new MailAddress("toolify.store@gmail.com", "Toolify Store"),
                Subject = "Ваш заказ успешно оформлен",
                Body = $@"
                Здравствуйте!
                Спасибо за покупку в магазине Toolify Store
                Номер вашего заказа: #{orderId}
                С уважением,
                Toolify Store",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);

            await smtp.SendMailAsync(message);
        }

        public async Task SendOrderConfirmedHtmlAsync(string toEmail, int orderId, string htmlBody)
        {
            var smtp = CreateClient();

            var message = new MailMessage
            {
                From = new MailAddress("toolify.store@gmail.com", "Toolify Store"),
                Subject = $"Ваш заказ №{orderId} оформлен",
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await smtp.SendMailAsync(message);
        }
        public async Task SendOrderStatusChangedAsync(
           string toEmail,
           int orderId,
           string? previousStatus,
           string newStatus,
           string? address,
           decimal totalAmount,
           IEnumerable<OrderLine>? lines)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var smtp = CreateClient();

            var safePrev = string.IsNullOrWhiteSpace(previousStatus) ? "—" : previousStatus.Trim();
            var safeNew = string.IsNullOrWhiteSpace(newStatus) ? "—" : newStatus.Trim();
            var safeAddress = string.IsNullOrWhiteSpace(address) ? "—" : address.Trim();

            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"font-family:Montserrat,Arial,sans-serif;line-height:1.5;color:#222;\">");
            sb.AppendLine("<h2 style=\"margin:0 0 10px;font-size:20px;\">Статус заказа обновлён</h2>");
            sb.AppendLine($"<p style=\"margin:0 0 14px;\">Мы обновили статус вашего заказа <b>#{orderId}</b>.</p>");
            sb.AppendLine("<div style=\"background:#f7f7f7;border:1px solid #eee;border-radius:12px;padding:14px 16px;\">");
            sb.AppendLine($"<div style=\"margin-bottom:8px;\"><span style=\"color:#666;\">Было:</span> <b>{WebUtility.HtmlEncode(safePrev)}</b></div>");
            sb.AppendLine($"<div style=\"margin-bottom:8px;\"><span style=\"color:#666;\">Стало:</span> <b>{WebUtility.HtmlEncode(safeNew)}</b></div>");
            sb.AppendLine($"<div style=\"margin-bottom:8px;\"><span style=\"color:#666;\">Адрес доставки:</span> <b>{WebUtility.HtmlEncode(safeAddress)}</b></div>");
            sb.AppendLine($"<div><span style=\"color:#666;\">Сумма заказа:</span> <b>{totalAmount:N2} BYN</b></div>");
            sb.AppendLine("</div>");

            if (lines != null)
            {
                var list = lines.Where(l => l != null).ToList();
                if (list.Count > 0)
                {
                    sb.AppendLine("<h3 style=\"margin:16px 0 10px;font-size:16px;\">Состав заказа</h3>");
                    sb.AppendLine("<table cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;border-collapse:collapse;border:1px solid #eee;border-radius:12px;overflow:hidden;\">");
                    sb.AppendLine("<thead><tr style=\"background:#fafafa;\">");
                    sb.AppendLine("<th align=\"left\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;\">Товар</th>");
                    sb.AppendLine("<th align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;white-space:nowrap;\">Кол-во</th>");
                    sb.AppendLine("<th align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;white-space:nowrap;\">Цена</th>");
                    sb.AppendLine("</tr></thead><tbody>");

                    foreach (var l in list)
                    {
                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;\">{WebUtility.HtmlEncode(l.Name ?? "—")}</td>");
                        sb.AppendLine($"<td align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;white-space:nowrap;\">{l.Quantity}</td>");
                        sb.AppendLine($"<td align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;white-space:nowrap;\">{l.Price:N2} BYN</td>");
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</tbody></table>");
                }
            }

            sb.AppendLine("<p style=\"margin:16px 0 0;color:#666;\">С уважением,<br/>Toolify Store</p>");
            sb.AppendLine("</div>");

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = $"Статус заказа #{orderId} изменён",
                Body = sb.ToString(),
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await smtp.SendMailAsync(message);
        }
        public async Task SendChatReplyAsync(string toEmail, string? subject, string replyText, int conversationId)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var smtp = CreateClient();
            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Вопрос по сайту" : subject.Trim();

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = $"Ответ администратора по чату #{conversationId}",
                Body = $@"
                    <div style=""font-family:Montserrat,Arial,sans-serif;line-height:1.6;color:#222;"">
                      <h2 style=""margin:0 0 12px;"">Вы получили ответ от администратора</h2>
                      <p style=""margin:0 0 12px;""><b>Тема:</b> {WebUtility.HtmlEncode(safeSubject)}</p>
                      <div style=""background:#f7f7f7;border:1px solid #eee;border-radius:12px;padding:14px;"">
                        {WebUtility.HtmlEncode(replyText).Replace("\n", "<br/>")}
                      </div>
                      <p style=""margin:16px 0 0;color:#666;"">С уважением,<br/>Toolify Store</p>
                    </div>",
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await smtp.SendMailAsync(message);
        }
    }
    public class OrderLine
    {
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}

