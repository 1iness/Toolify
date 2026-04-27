using HouseholdStore.Models;
using System.Data;
using System.Data.SqlClient;
using Toolify.AuthService.Models;
using Toolify.ProductService.Database;
using Toolify.ProductService.Models;

namespace HouseholdStore.Services;

public class AdminReportBuilder
{
    private readonly AuthApiService _authApi;
    private readonly SqlConnectionFactory _connectionFactory;

    public AdminReportBuilder(AuthApiService authApi, SqlConnectionFactory connectionFactory)
    {
        _authApi = authApi;
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminReportsViewModel> BuildAsync(AdminReportsFilter filter)
    {
        NormalizeFilter(filter);

        var categories = await GetCategoriesAsync();
        var users = await TryGetUsersAsync();
        var salesCategoryIds = filter.SalesCategoryIds.Where(id => id > 0).ToHashSet();
        var popularityCategoryIds = filter.PopularityCategoryIds.Where(id => id > 0).ToHashSet();

        var salesLines = (await GetOrderLinesAsync(filter.SalesStartDate, filter.SalesEndDate))
            .Where(line => salesCategoryIds.Count == 0 || salesCategoryIds.Contains(line.CategoryId))
            .ToList();
        var averageOrders = await GetPurchaseOrdersAsync(filter.AverageCheckStartDate, filter.AverageCheckEndDate);
        var popularityLines = (await GetOrderLinesAsync(filter.PopularityStartDate, filter.PopularityEndDate))
            .Where(line => popularityCategoryIds.Count == 0 || popularityCategoryIds.Contains(line.CategoryId))
            .ToList();
        var customerOrders = await GetPurchaseOrdersAsync(filter.CustomerStartDate, filter.CustomerEndDate);
        var totalAmount = averageOrders.Sum(o => o.TotalAmount);

        return new AdminReportsViewModel
        {
            Filter = filter,
            Categories = categories.OrderBy(c => c.Name).ToList(),
            SalesByCategory = BuildSalesRows(salesLines, categories, salesCategoryIds),
            AverageCheck = new AverageCheckReport
            {
                OrderCount = averageOrders.Count,
                TotalAmount = totalAmount,
                AverageAmount = averageOrders.Count == 0 ? 0m : totalAmount / averageOrders.Count
            },
            ProductPopularity = BuildPopularityRows(popularityLines),
            CustomerPurchaseHistory = BuildCustomerRows(customerOrders, users),
            SalesPeriodTitle = BuildPeriodTitle(filter.SalesStartDate, filter.SalesEndDate),
            AverageCheckPeriodTitle = BuildPeriodTitle(filter.AverageCheckStartDate, filter.AverageCheckEndDate),
            PopularityPeriodTitle = BuildPeriodTitle(filter.PopularityStartDate, filter.PopularityEndDate),
            CustomerPeriodTitle = BuildPeriodTitle(filter.CustomerStartDate, filter.CustomerEndDate)
        };
    }

    public IReadOnlyList<AdminReportTable> BuildTables(AdminReportsViewModel model, string? reportType = null)
    {
        var tables = new List<AdminReportTable>
        {
            new()
            {
                Title = $"Отчет по продажам за {model.SalesPeriodTitle}",
                Headers = new[] { "Категория", "Заказов", "Продано товаров", "Сумма продаж, BYN" },
                Rows = model.SalesByCategory
                    .Select(r => Row(r.CategoryName, r.OrderCount, r.ItemsSold, Money(r.SalesAmount)))
                    .ToList()
            },
            new()
            {
                Title = $"Отчет о средней сумме чека за {model.AverageCheckPeriodTitle}",
                Headers = new[] { "Количество заказов", "Общая сумма, BYN", "Средняя сумма чека, BYN" },
                Rows = new List<IReadOnlyList<string>>
                {
                    Row(model.AverageCheck.OrderCount, Money(model.AverageCheck.TotalAmount), Money(model.AverageCheck.AverageAmount))
                }
            },
            new()
            {
                Title = $"Отчет по популярности товаров за {model.PopularityPeriodTitle}",
                Headers = new[] { "Категория", "Товар", "Продано, шт.", "Заказов", "Сумма продаж, BYN" },
                Rows = model.ProductPopularity
                    .Select(r => Row(r.CategoryName, r.ProductName, r.QuantitySold, r.OrderCount, Money(r.SalesAmount)))
                    .ToList()
            },
            new()
            {
                Title = $"История количества покупок в разрезе покупателя за {model.CustomerPeriodTitle}",
                Headers = new[] { "Покупатель", "Email", "Заказов", "Куплено товаров", "Общая сумма, BYN", "Средний чек, BYN", "Последняя покупка" },
                Rows = model.CustomerPurchaseHistory
                    .Select(r => Row(
                        r.CustomerName,
                        r.Email,
                        r.OrderCount,
                        r.ItemsPurchased,
                        Money(r.TotalAmount),
                        Money(r.AverageOrderAmount),
                        r.LastOrderDate?.ToString("dd.MM.yyyy") ?? "-"))
                    .ToList()
            }
        };

        return (reportType ?? "all").ToLowerInvariant() switch
        {
            "sales" => tables.Take(1).ToList(),
            "average" => tables.Skip(1).Take(1).ToList(),
            "popularity" => tables.Skip(2).Take(1).ToList(),
            "customers" => tables.Skip(3).Take(1).ToList(),
            _ => tables
        };
    }

    public static bool HasInvalidDateRanges(AdminReportsFilter filter, out string message)
    {
        if (IsInvalidRange(filter.SalesStartDate, filter.SalesEndDate))
        {
            message = "В отчете по продажам начальная дата не может быть больше конечной.";
            return true;
        }

        if (IsInvalidRange(filter.AverageCheckStartDate, filter.AverageCheckEndDate))
        {
            message = "В отчете по среднему чеку начальная дата не может быть больше конечной.";
            return true;
        }

        if (IsInvalidRange(filter.PopularityStartDate, filter.PopularityEndDate))
        {
            message = "В отчете по популярности начальная дата не может быть больше конечной.";
            return true;
        }

        if (IsInvalidRange(filter.CustomerStartDate, filter.CustomerEndDate))
        {
            message = "В истории покупателей начальная дата не может быть больше конечной.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    private async Task<List<Category>> GetCategoriesAsync()
    {
        var categories = new List<Category>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("sp_AdminReports_GetCategories", connection) { CommandType = CommandType.StoredProcedure };
        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name"))
            });
        }

        return categories;
    }

    private async Task<List<ReportOrderLine>> GetOrderLinesAsync(DateTime? startDate, DateTime? endDate)
    {
        var lines = new List<ReportOrderLine>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("sp_AdminReports_GetOrderLines", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@StartDate", (object?)startDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", (object?)endDate?.Date ?? DBNull.Value);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lines.Add(new ReportOrderLine(
                reader.GetInt32(reader.GetOrdinal("OrderId")),
                reader.GetInt32(reader.GetOrdinal("ProductId")),
                reader.GetString(reader.GetOrdinal("ProductName")),
                reader.GetInt32(reader.GetOrdinal("CategoryId")),
                reader.GetString(reader.GetOrdinal("CategoryName")),
                reader.GetInt32(reader.GetOrdinal("Quantity")),
                reader.GetDecimal(reader.GetOrdinal("Price"))));
        }

        return lines;
    }

    private async Task<List<PurchaseOrderReportRow>> GetPurchaseOrdersAsync(DateTime? startDate, DateTime? endDate)
    {
        var orders = new List<PurchaseOrderReportRow>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand("sp_AdminReports_GetPurchaseOrders", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@StartDate", (object?)startDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", (object?)endDate?.Date ?? DBNull.Value);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(new PurchaseOrderReportRow
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                GuestFirstName = reader.IsDBNull(reader.GetOrdinal("GuestFirstName")) ? null : reader.GetString(reader.GetOrdinal("GuestFirstName")),
                GuestLastName = reader.IsDBNull(reader.GetOrdinal("GuestLastName")) ? null : reader.GetString(reader.GetOrdinal("GuestLastName")),
                GuestEmail = reader.IsDBNull(reader.GetOrdinal("GuestEmail")) ? null : reader.GetString(reader.GetOrdinal("GuestEmail")),
                GuestPhone = reader.IsDBNull(reader.GetOrdinal("GuestPhone")) ? null : reader.GetString(reader.GetOrdinal("GuestPhone")),
                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                ItemsPurchased = reader.GetInt32(reader.GetOrdinal("ItemsPurchased"))
            });
        }

        return orders;
    }

    private async Task<List<User>> TryGetUsersAsync()
    {
        try
        {
            return await _authApi.GetAllUsersAsync();
        }
        catch
        {
            return new List<User>();
        }
    }

    private static List<SalesByCategoryReportRow> BuildSalesRows(
        IReadOnlyList<ReportOrderLine> lines,
        IReadOnlyList<Category> categories,
        IReadOnlySet<int> selectedCategoryIds)
    {
        var rowsByCategory = lines
            .GroupBy(line => new { line.CategoryId, line.CategoryName })
            .ToDictionary(
                group => group.Key.CategoryId,
                group => new SalesByCategoryReportRow
                {
                    CategoryId = group.Key.CategoryId,
                    CategoryName = group.Key.CategoryName,
                    OrderCount = group.Select(line => line.OrderId).Distinct().Count(),
                    ItemsSold = group.Sum(line => line.Quantity),
                    SalesAmount = group.Sum(line => line.LineAmount)
                });

        var visibleCategories = categories
            .Where(category => selectedCategoryIds.Count == 0 || selectedCategoryIds.Contains(category.Id))
            .OrderBy(category => category.Name);

        var result = visibleCategories
            .Select(category => rowsByCategory.TryGetValue(category.Id, out var row)
                ? row
                : new SalesByCategoryReportRow { CategoryId = category.Id, CategoryName = category.Name })
            .ToList();

        if (rowsByCategory.TryGetValue(0, out var uncategorized))
        {
            result.Add(uncategorized);
        }

        return result;
    }

    private static List<ProductPopularityReportRow> BuildPopularityRows(IReadOnlyList<ReportOrderLine> lines)
    {
        return lines
            .GroupBy(line => new { line.CategoryId, line.CategoryName, line.ProductId, line.ProductName })
            .Select(group => new ProductPopularityReportRow
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                QuantitySold = group.Sum(line => line.Quantity),
                OrderCount = group.Select(line => line.OrderId).Distinct().Count(),
                SalesAmount = group.Sum(line => line.LineAmount)
            })
            .OrderBy(row => row.CategoryName)
            .ThenByDescending(row => row.QuantitySold)
            .ThenByDescending(row => row.SalesAmount)
            .ToList();
    }

    private static List<CustomerPurchaseHistoryReportRow> BuildCustomerRows(IReadOnlyList<PurchaseOrderReportRow> orders, IReadOnlyList<User> users)
    {
        var userById = users.ToDictionary(u => u.Id);

        return orders
            .GroupBy(order => BuildCustomerKey(order))
            .Select(group =>
            {
                var firstOrder = group.OrderByDescending(o => o.OrderDate).First();
                var customer = ResolveCustomer(firstOrder, userById);
                var total = group.Sum(o => o.TotalAmount);
                var count = group.Count();

                return new CustomerPurchaseHistoryReportRow
                {
                    CustomerName = customer.Name,
                    Email = customer.Email,
                    OrderCount = count,
                    ItemsPurchased = group.Sum(o => o.ItemsPurchased),
                    TotalAmount = total,
                    AverageOrderAmount = count == 0 ? 0m : total / count,
                    LastOrderDate = group.Max(o => o.OrderDate)
                };
            })
            .OrderByDescending(row => row.OrderCount)
            .ThenByDescending(row => row.TotalAmount)
            .ToList();
    }

    private static string BuildCustomerKey(PurchaseOrderReportRow order)
    {
        if (order.UserId.HasValue) return $"user:{order.UserId.Value}";
        if (!string.IsNullOrWhiteSpace(order.GuestEmail)) return $"email:{order.GuestEmail.Trim().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(order.GuestPhone)) return $"phone:{order.GuestPhone.Trim()}";
        return $"order:{order.Id}";
    }

    private static (string Name, string Email) ResolveCustomer(PurchaseOrderReportRow order, IReadOnlyDictionary<int, User> userById)
    {
        if (order.UserId.HasValue && userById.TryGetValue(order.UserId.Value, out var user))
        {
            var name = $"{user.FirstName} {user.LastName}".Trim();
            return (string.IsNullOrWhiteSpace(name) ? user.Email : name, user.Email);
        }

        var guestName = $"{order.GuestFirstName} {order.GuestLastName}".Trim();
        return (
            string.IsNullOrWhiteSpace(guestName) ? "Гость" : guestName,
            string.IsNullOrWhiteSpace(order.GuestEmail) ? "-" : order.GuestEmail);
    }

    private static void NormalizeFilter(AdminReportsFilter filter)
    {
        filter.SalesCategoryIds ??= Array.Empty<int>();
        filter.PopularityCategoryIds ??= Array.Empty<int>();
    }

    private static bool IsInvalidRange(DateTime? startDate, DateTime? endDate)
    {
        return startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date;
    }

    private static string BuildPeriodTitle(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
            return $"{startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
        if (startDate.HasValue)
            return $"с {startDate.Value:dd.MM.yyyy}";
        if (endDate.HasValue)
            return $"по {endDate.Value:dd.MM.yyyy}";
        return "весь период";
    }

    private static string Money(decimal value)
    {
        return value.ToString("N2");
    }

    private static IReadOnlyList<string> Row(params object?[] values)
    {
        return values.Select(value => value?.ToString() ?? string.Empty).ToList();
    }

    private sealed record ReportOrderLine(
        int OrderId,
        int ProductId,
        string ProductName,
        int CategoryId,
        string CategoryName,
        int Quantity,
        decimal UnitPrice)
    {
        public decimal LineAmount => UnitPrice * Quantity;
    }

    private sealed class PurchaseOrderReportRow
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? GuestFirstName { get; set; }
        public string? GuestLastName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsPurchased { get; set; }
    }
}
