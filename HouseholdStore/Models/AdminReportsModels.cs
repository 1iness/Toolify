using Toolify.ProductService.Models;

namespace HouseholdStore.Models;

public class AdminReportsFilter
{
    public DateTime? SalesStartDate { get; set; }
    public DateTime? SalesEndDate { get; set; }
    public int[] SalesCategoryIds { get; set; } = Array.Empty<int>();
    public DateTime? AverageCheckStartDate { get; set; }
    public DateTime? AverageCheckEndDate { get; set; }
    public DateTime? PopularityStartDate { get; set; }
    public DateTime? PopularityEndDate { get; set; }
    public int[] PopularityCategoryIds { get; set; } = Array.Empty<int>();
    public DateTime? CustomerStartDate { get; set; }
    public DateTime? CustomerEndDate { get; set; }
}

public class AdminReportsViewModel
{
    public AdminReportsFilter Filter { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<SalesByCategoryReportRow> SalesByCategory { get; set; } = new();
    public AverageCheckReport AverageCheck { get; set; } = new();
    public List<ProductPopularityReportRow> ProductPopularity { get; set; } = new();
    public List<CustomerPurchaseHistoryReportRow> CustomerPurchaseHistory { get; set; } = new();
    public string SalesPeriodTitle { get; set; } = string.Empty;
    public string AverageCheckPeriodTitle { get; set; } = string.Empty;
    public string PopularityPeriodTitle { get; set; } = string.Empty;
    public string CustomerPeriodTitle { get; set; } = string.Empty;
}

public class SalesByCategoryReportRow
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int ItemsSold { get; set; }
    public decimal SalesAmount { get; set; }
}

public class AverageCheckReport
{
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
}

public class ProductPopularityReportRow
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public int OrderCount { get; set; }
    public decimal SalesAmount { get; set; }
}

public class CustomerPurchaseHistoryReportRow
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int ItemsPurchased { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class AdminReportTable
{
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
}
