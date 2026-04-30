using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Toolify.ProductService.Models;

namespace HouseholdStore.Services
{
    public static class OrderConfirmationEmailHtmlBuilder
    {
        private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        public static string Build(OrderEmailPayload payload)
        {
            var h = payload.Header;
            var lines = payload.Lines;

            var goodsListSum = lines.Sum(l => l.LineTotalList);
            var goodsPaidSum = lines.Sum(l => l.LineTotalPaid);
            var discountTotal = Math.Max(0m, goodsListSum - goodsPaidSum);

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" /></head>");
            sb.Append("<body style=\"margin:0;padding:16px;background:#f4f4f4;font-family:'Segoe UI',Arial,sans-serif;color:#222;\">");
            sb.Append("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" width=\"100%\" style=\"max-width:640px;margin:0 auto;background:#ffffff;border-radius:12px;padding:24px;\">");
            sb.Append("<tr><td>");

            sb.Append("<p style=\"margin:0 0 12px 0;font-size:16px;\">Здравствуйте!</p>");
            sb.Append("<p style=\"margin:0 0 20px 0;font-size:16px;\">Номер вашего заказа: <strong>#")
                .Append(h.OrderId).Append("</strong></p>");

            foreach (var line in lines)
            {
                var name = WebUtility.HtmlEncode(line.ProductName);
                var hasLineDiscount = line.LineTotalList > line.LineTotalPaid + 0.005m;

                sb.Append("<div style=\"margin-bottom:20px;padding-bottom:16px;border-bottom:1px solid #eee;font-size:15px;line-height:1.45;\">");
                sb.Append("<div style=\"font-weight:600;margin-bottom:6px;\">").Append(name).Append("</div>");
                sb.Append("<div style=\"color:#555;font-size:14px;margin-bottom:8px;\">Количество: ")
                    .Append(line.Quantity).Append(" шт.</div>");

                sb.Append("<div style=\"font-size:14px;\"><span style=\"color:#555;margin-right:6px;\">Цена позиции:</span>");
                if (hasLineDiscount)
                {
                    sb.Append("<span style=\"text-decoration:line-through;color:#888;margin-right:8px;\">")
                        .Append(Byn(line.LineTotalList)).Append("</span>");
                    sb.Append("<strong style=\"color:#1a7f37;\">").Append(Byn(line.LineTotalPaid)).Append("</strong>");
                }
                else
                    sb.Append("<strong>").Append(Byn(line.LineTotalPaid)).Append("</strong>");
                sb.Append("</div>");

                sb.Append("</div>");
            }

            sb.Append("<div style=\"margin:24px 0 16px 0;padding:16px;background:#f8faf8;border-radius:8px;font-size:15px;line-height:1.6;\">");
            sb.Append("<div style=\"display:flex;justify-content:space-between;margin-bottom:8px;\"><span>Итого:</span><strong>")
                .Append(Byn(goodsPaidSum)).Append("</strong></div>");

            var deliveryTypeHtml = FormatDeliveryTypeSummary(h);
            sb.Append("<div style=\"display:flex;justify-content:space-between;margin-bottom:8px;\"><span>Доставка:</span><span>")
                .Append(deliveryTypeHtml).Append("</span></div>");

            if (discountTotal >= 0.01m)
            {
                sb.Append("<div style=\"display:flex;justify-content:space-between;margin-bottom:8px;color:#1a7f37;\"><span>Скидка:</span><strong>−")
                    .Append(Byn(discountTotal)).Append("</strong></div>");
                if (!string.IsNullOrWhiteSpace(h.PromoCode))
                {
                    sb.Append("<div style=\"font-size:13px;color:#555;margin-top:4px;\">Промокод: <strong>")
                        .Append(WebUtility.HtmlEncode(h.PromoCode)).Append("</strong>");
                    if (h.PromoDiscountPercent > 0)
                        sb.Append(" (−").Append(h.PromoDiscountPercent).Append("%)");
                    sb.Append("</div>");
                }
            }
            else if (!string.IsNullOrWhiteSpace(h.PromoCode))
            {
                sb.Append("<div style=\"display:flex;justify-content:space-between;margin-bottom:8px;color:#555;\"><span>Промокод:</span><strong>")
                    .Append(WebUtility.HtmlEncode(h.PromoCode)).Append("</strong>");
                if (h.PromoDiscountPercent > 0)
                    sb.Append("<span style=\"margin-left:6px;\">(−").Append(h.PromoDiscountPercent).Append("%)</span>");
                sb.Append("</div>");
            }

            sb.Append("<hr style=\"border:none;border-top:1px solid #dde8dd;margin:12px 0;\" />");
            sb.Append("<div style=\"display:flex;justify-content:space-between;font-size:17px;\"><span>К оплате:</span><strong>")
                .Append(Byn(h.TotalAmount)).Append("</strong></div>");
            sb.Append("</div>");

            sb.Append("<h2 style=\"font-size:17px;margin:24px 0 12px 0;\">Информация о доставке</h2>");
            sb.Append("<div style=\"font-size:15px;line-height:1.65;\">");
            sb.Append("<p style=\"margin:0 0 8px 0;\"><strong>Адрес доставки:</strong><br />")
                .Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(h.Address) ? "—" : h.Address)).Append("</p>");
            sb.Append("<p style=\"margin:0 0 8px 0;\"><strong>Контактный телефон:</strong><br />")
                .Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(h.GuestPhone) ? "—" : h.GuestPhone)).Append("</p>");
            sb.Append("<p style=\"margin:0 0 8px 0;\"><strong>Способ оплаты:</strong><br />")
                .Append(WebUtility.HtmlEncode(FormatPaymentMethod(h.PaymentMethod))).Append("</p>");
            sb.Append("</div>");

            sb.Append("""
                </td></tr></table>
                <p style="max-width:640px;margin:16px auto 0 auto;font-size:12px;color:#888;text-align:center;">С уважением, Toolify Store</p>
                </body></html>
                """);

            return sb.ToString();
        }

        private static string Byn(decimal value) =>
            value.ToString("N2", Ru) + "&nbsp;р.";

        private static string FormatDeliveryTypeSummary(OrderEmailDetails h)
        {
            if (string.Equals(h.DeliveryType, "Pickup", StringComparison.OrdinalIgnoreCase))
                return "Самовывоз, бесплатно";

            if (h.DeliveryFee <= 0.001m)
                return "Курьером до двери, бесплатно";

            return "Курьером до двери, доплата " + Byn(h.DeliveryFee);
        }

        private static string FormatPaymentMethod(string code)
        {
            return code switch
            {
                "CardOnDelivery" => "Картой при получении",
                "CashOnDelivery" => "Наличными при получении",
                _ => string.IsNullOrEmpty(code) ? "—" : code
            };
        }
    }
}
