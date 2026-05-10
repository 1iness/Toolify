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

            sb.AppendLine("<h3 style=\"margin:18px 0 10px;font-size:16px;\">Товары в этом заказе</h3>");
            if (lines != null)
            {
                var list = lines.Where(l => l != null).ToList();
                if (list.Count > 0)
                {
                    sb.AppendLine("<table cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:640px;border-collapse:collapse;border:1px solid #eee;border-radius:12px;overflow:hidden;\">");
                    sb.AppendLine("<thead><tr style=\"background:#fafafa;\">");
                    sb.AppendLine("<th align=\"left\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;\">Товар</th>");
                    sb.AppendLine("<th align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;white-space:nowrap;\">Кол-во</th>");
                    sb.AppendLine("<th align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;white-space:nowrap;\">Цена за шт.</th>");
                    sb.AppendLine("<th align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #eee;font-size:12px;color:#666;white-space:nowrap;\">Сумма</th>");
                    sb.AppendLine("</tr></thead><tbody>");

                    foreach (var l in list)
                    {
                        var unit = l.Price;
                        var rowTotal = l.LineTotal ?? l.Quantity * l.Price;
                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;\">{WebUtility.HtmlEncode(l.Name ?? "—")}</td>");
                        sb.AppendLine($"<td align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;white-space:nowrap;\">{l.Quantity}</td>");
                        sb.AppendLine($"<td align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;white-space:nowrap;\">{unit:N2} BYN</td>");
                        sb.AppendLine($"<td align=\"right\" style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0;white-space:nowrap;\">{rowTotal:N2} BYN</td>");
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</tbody></table>");
                    sb.AppendLine("<p style=\"margin:12px 0 0;color:#666;font-size:13px;line-height:1.45;\">Если нужны уточнения по статусу — ответьте на это письмо или свяжитесь с поддержкой магазина.</p>");
                }
                else
                {
                    sb.AppendLine($"<p style=\"margin:0;color:#444;font-size:14px;line-height:1.5;\">Состав вашего заказа <b>№{orderId}</b> по данным нашей базы временно недоступен в письме. Ориентируйтесь на сумму заказа выше; при необходимости запросите детали в поддержке.</p>");
                }
            }
            else
            {
                sb.AppendLine($"<p style=\"margin:0;color:#444;font-size:14px;line-height:1.5;\">Состав вашего заказа <b>№{orderId}</b> не был указан.</p>");
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
        public async Task SendChatReplyAsync(
            string toEmail,
            string? subject,
            string replyText,
            int conversationId,
            string? customerLastMessage = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var smtp = CreateClient();
            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Обращение в поддержку" : subject.Trim();

            string questionBlock = string.Empty;
            if (!string.IsNullOrWhiteSpace(customerLastMessage))
            {
                var q = WebUtility.HtmlEncode(customerLastMessage.Trim())
                    .Replace("\r\n", "<br/>")
                    .Replace("\n", "<br/>");
                questionBlock = $@"
                      <p style=""margin:18px 0 8px;font-weight:600;"">Ваше сообщение</p>
                      <div style=""background:#fff;border:1px solid #e5e5e5;border-radius:12px;padding:14px;margin-bottom:4px;"">
                        {q}
                      </div>";
            }

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = $"Ответ администратора по чату #{conversationId}",
                Body = $@"
                    <div style=""font-family:Montserrat,Arial,sans-serif;line-height:1.6;color:#222;"">
                      <h2 style=""margin:0 0 12px;"">Вы получили ответ от администратора</h2>
                      <p style=""margin:0 0 12px;""><b>Тема:</b> {WebUtility.HtmlEncode(safeSubject)}</p>
                      {questionBlock}
                      <p style=""margin:16px 0 8px;font-weight:600;"">Ответ</p>
                      <div style=""background:#f7f7f7;border:1px solid #eee;border-radius:12px;padding:14px;"">
                        {WebUtility.HtmlEncode(replyText).Replace("\r\n", "<br/>").Replace("\n", "<br/>")}
                      </div>
                      <p style=""margin:16px 0 0;color:#666;"">С уважением,<br/>Toolify Store</p>
                    </div>",
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            await smtp.SendMailAsync(message);
        }

        public async Task SendIncomingUserChatToAdminAsync(
            int conversationId,
            string senderLabel,
            string? contactEmail,
            string? subject,
            string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText)) return;

            var smtp = CreateClient();
            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Обращение в поддержку" : subject.Trim();
            var safeSender = string.IsNullOrWhiteSpace(senderLabel) ? "Неизвестно" : senderLabel.Trim();
            var safeContact = string.IsNullOrWhiteSpace(contactEmail) ? "—" : contactEmail.Trim();
            var safeBody = string.IsNullOrWhiteSpace(messageText) ? "—" : messageText.Trim();

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store — чат"),
                Subject = $"Новое сообщение в чате #{conversationId}: {safeSender}",
                Body = $@"
                    <div style=""font-family:Montserrat,Arial,sans-serif;line-height:1.6;color:#222;"">
                      <h2 style=""margin:0 0 12px;"">Сообщение в чате с клиентом</h2>
                      <p style=""margin:0 0 8px;""><b>Диалог:</b> #{conversationId}</p>
                      <p style=""margin:0 0 8px;""><b>Кто пишет:</b> {WebUtility.HtmlEncode(safeSender)}</p>
                      <p style=""margin:0 0 8px;""><b>Контакт (email в чате):</b> {WebUtility.HtmlEncode(safeContact)}</p>
                      <p style=""margin:0 0 12px;""><b>Тема:</b> {WebUtility.HtmlEncode(safeSubject)}</p>
                      <div style=""background:#f7f7f7;border:1px solid #eee;border-radius:12px;padding:14px;"">
                        {WebUtility.HtmlEncode(safeBody).Replace("\r\n", "<br/>").Replace("\n", "<br/>")}
                      </div>
                      <p style=""margin:16px 0 0;color:#666;font-size:13px;"">Ответьте клиенту в админ-панели (раздел «Чат с клиентами»).</p>
                    </div>",
                IsBodyHtml = true
            };

            message.To.Add(_email);
            await smtp.SendMailAsync(message);
        }

        public async Task SendMarketingItemCreatedAsync(string toEmail, string itemTitle, string itemName)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var smtp = CreateClient();
            var safeTitle = string.IsNullOrWhiteSpace(itemTitle) ? "новое предложение" : itemTitle.Trim();
            var safeName = string.IsNullOrWhiteSpace(itemName) ? "Без названия" : itemName.Trim();

            var message = new MailMessage
            {
                From = new MailAddress(_email, "Toolify Store"),
                Subject = $"Toolify: {safeTitle} «{safeName}»",
                Body = $@"
                    <div style=""font-family:Montserrat,Arial,sans-serif;line-height:1.6;color:#222;"">
                      <h2 style=""margin:0 0 12px;"">В Toolify появилось новое предложение</h2>
                      <p style=""margin:0 0 12px;"">Мы добавили: <b>{WebUtility.HtmlEncode(safeTitle)}</b></p>
                      <div style=""background:#f7f7f7;border:1px solid #eee;border-radius:12px;padding:14px;"">
                        Название: <b>{WebUtility.HtmlEncode(safeName)}</b>
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
        public decimal? LineTotal { get; set; }
    }
}

