using System.Data;
using System.Data.SqlClient;
using Toolify.ProductService.Database;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Data
{
    public class ProductRepository
    {
        private readonly SqlConnectionFactory _factory;

        public ProductRepository(SqlConnectionFactory factory)
        {
            _factory = factory;
        }
        public async Task<List<Product>> GetAllAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetAllProducts", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();

            var products = new List<Product>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                    var product = products.FirstOrDefault(p => p.Id == productId);

                    if (product != null)
                    {
                        product.Configurations.Add(new ProductConfiguration
                        {
                            FeatureId = reader.GetInt32(reader.GetOrdinal("FeatureId")),
                            FeatureName = reader.GetString(reader.GetOrdinal("FeatureName")),
                            FeatureValue = reader.GetString(reader.GetOrdinal("FeatureValue"))
                        });
                    }
                }
            }

            return products;
        }

        public async Task<List<Product>> SearchAsync(string term)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_SearchProducts", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Term", term);

            await connection.OpenAsync();
            var products = new List<Product>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) products.Add(MapProduct(reader));
            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetProductById", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;
            var product = MapProduct(reader);

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync()) product.Configurations.Add(new ProductConfiguration
                {
                    FeatureId = reader.GetInt32(reader.GetOrdinal("FeatureId")),
                    FeatureName = reader.GetString(reader.GetOrdinal("FeatureName")),
                    FeatureValue = reader.GetString(reader.GetOrdinal("FeatureValue"))
                });
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync()) product.Images.Add(new ProductImage
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ImageData = (byte[])reader["ImageData"],
                    ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                    IsMain = reader.GetBoolean(reader.GetOrdinal("IsMain"))
                });
            }
            return product;
        }

        public async Task<int> AddAsync(Product product)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddProduct", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@ShortDescription", (object?)product.ShortDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@FullDescription", (object?)product.FullDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@Discount", 0);
            command.Parameters.AddWithValue("@ArticleNumber", (object?)product.ArticleNumber ?? DBNull.Value);

            var featureTable = new DataTable();
            featureTable.Columns.Add("FeatureId", typeof(int));
            featureTable.Columns.Add("FeatureValue", typeof(string));
            foreach (var config in product.Configurations)
            {
                featureTable.Rows.Add(config.FeatureId, config.FeatureValue);
            }
            var featureParam = command.Parameters.AddWithValue("@Features", featureTable);
            featureParam.SqlDbType = SqlDbType.Structured;
            featureParam.TypeName = "dbo.FeatureTableType";

            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_UpdateProduct", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@ShortDescription", (object?)product.ShortDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@FullDescription", (object?)product.FullDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@Discount", 0);
            command.Parameters.AddWithValue("@ArticleNumber", (object?)product.ArticleNumber ?? DBNull.Value);

            var featureTable = new DataTable();
            featureTable.Columns.Add("FeatureId", typeof(int));
            featureTable.Columns.Add("FeatureValue", typeof(string));

            if (product.Configurations != null)
            {
                foreach (var config in product.Configurations)
                {
                    featureTable.Rows.Add(config.FeatureId, config.FeatureValue);
                }
            }

            var featureParam = command.Parameters.AddWithValue("@Features", featureTable);
            featureParam.SqlDbType = SqlDbType.Structured;
            featureParam.TypeName = "dbo.FeatureTableType";

            await connection.OpenAsync();
            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_DeleteProduct", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetAllCategories", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();

            var categories = new List<Category>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) categories.Add(new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name"))
            });
            return categories;
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddCategory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Name", category.Name);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Category
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? string.Empty
                };
            }

            throw new Exception("Ошибка при создании или получении категории.");
        }

        public async Task AddToCartAsync(int productId, int? userId, string? guestId, int quantity = 1)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddToCart", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quantity", quantity);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int? userId, string? guestId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetCartItems", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);

            await connection.OpenAsync();
            var raw = new List<(int ProductId, string Name, decimal ListPrice, int Quantity, string? Article)>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var listPrice = reader.GetDecimal(reader.GetOrdinal("Price"));
                    raw.Add((
                        reader.GetInt32(reader.GetOrdinal("ProductId")),
                        reader.GetString(reader.GetOrdinal("Name")),
                        listPrice,
                        reader.GetInt32(reader.GetOrdinal("Quantity")),
                        reader.IsDBNull(reader.GetOrdinal("ArticleNumber")) ? null : reader.GetString(reader.GetOrdinal("ArticleNumber"))
                    ));
                }
            }

            if (raw.Count == 0) return new List<CartItem>();

            var categoryByProduct = await GetCategoryIdsByProductIdsAsync(raw.Select(r => r.ProductId).Distinct().ToList());
            var rules = await GetDiscountRulesAsync();
            var campaigns = await GetDiscountCampaignsAsync();
            var now = DateTime.Now;
            var clientPct = ResolveBestClientPercent(userId, rules, campaigns, now);

            var items = new List<CartItem>();
            foreach (var row in raw)
            {
                categoryByProduct.TryGetValue(row.ProductId, out var categoryId);
                var prodPct = ResolveBestProductPercent(row.ProductId, rules, campaigns, now);
                var catPct = ResolveBestCategoryPercent(categoryId, rules, campaigns, now);
                var stackPct = Math.Max(prodPct, Math.Max(catPct, clientPct));
                var percentTotal = row.ListPrice * row.Quantity * (1 - stackPct / 100m);

                var bundle = ResolveBestProductBundle(row.ProductId, rules, campaigns, now);
                var bundleTotal = bundle == null
                    ? decimal.MaxValue
                    : CalcBundleTotal(row.ListPrice, row.Quantity, bundle.BuyQty, bundle.PayQty);

                var bestTotal = Math.Min(percentTotal, bundleTotal);
                var finalUnit = Math.Round(bestTotal / row.Quantity, 2, MidpointRounding.AwayFromZero);

                decimal? oldPrice = null;
                if (finalUnit < row.ListPrice - 0.005m)
                    oldPrice = row.ListPrice;

                items.Add(new CartItem
                {
                    ProductId = row.ProductId,
                    ProductName = row.Name,
                    Price = finalUnit,
                    OldPrice = oldPrice,
                    Quantity = row.Quantity,
                    ArticleNumber = row.Article
                });
            }

            return items;
        }

        private sealed record BundleSpec(int BuyQty, int PayQty, int Priority, int RuleId);

        private static decimal CalcBundleTotal(decimal unitPrice, int qty, int buyQty, int payQty)
        {
            if (qty <= 0) return 0;
            if (buyQty < 2 || payQty < 1 || payQty >= buyQty) return unitPrice * qty;
            var groups = qty / buyQty;
            var rem = qty % buyQty;
            var payable = groups * payQty + rem;
            return unitPrice * payable;
        }

        private async Task<Dictionary<int, int>> GetCategoryIdsByProductIdsAsync(List<int> productIds)
        {
            var dict = new Dictionary<int, int>();
            if (productIds == null || productIds.Count == 0) return dict;
            var distinct = productIds.Distinct().ToList();
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();
            var idList = string.Join(",", distinct);
            using var cmd = new SqlCommand($"SELECT Id, CategoryId FROM dbo.Products WHERE Id IN ({idList})", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                dict[reader.GetInt32(0)] = reader.GetInt32(1);
            return dict;
        }

        private static decimal ResolveBestCategoryPercent(int categoryId, List<DiscountRule> rules, List<DiscountCampaign> campaigns, DateTime now)
        {
            bool IsCampaignActive(DiscountCampaign dc) =>
                dc.IsActive && now >= dc.StartDate && now <= dc.EndDate;

            var candidates = new List<(decimal val, int prio, int rid)>();
            foreach (var r in rules)
            {
                if (!r.IsActive || r.ScopeType != "Category" || r.DiscountMode != "Percent" || r.CategoryId != categoryId)
                    continue;
                if (r.CampaignId.HasValue)
                {
                    var dc = campaigns.FirstOrDefault(c => c.Id == r.CampaignId.Value);
                    if (dc == null || !IsCampaignActive(dc)) continue;
                    candidates.Add((r.DiscountValue, dc.Priority, r.Id));
                }
                else
                    candidates.Add((r.DiscountValue, 0, r.Id));
            }
            if (candidates.Count == 0) return 0;
            return candidates.OrderByDescending(x => x.prio).ThenByDescending(x => x.rid).First().val;
        }

        private static decimal ResolveBestProductPercent(int productId, List<DiscountRule> rules, List<DiscountCampaign> campaigns, DateTime now)
        {
            bool IsCampaignActive(DiscountCampaign dc) =>
                dc.IsActive && now >= dc.StartDate && now <= dc.EndDate;

            var candidates = new List<(decimal val, int prio, int rid)>();
            foreach (var r in rules)
            {
                if (!r.IsActive || r.ScopeType != "Product" || r.DiscountMode != "Percent" || r.ProductId != productId)
                    continue;
                if (r.CampaignId.HasValue)
                {
                    var dc = campaigns.FirstOrDefault(c => c.Id == r.CampaignId.Value);
                    if (dc == null || !IsCampaignActive(dc)) continue;
                    candidates.Add((r.DiscountValue, dc.Priority, r.Id));
                }
                else
                    candidates.Add((r.DiscountValue, 0, r.Id));
            }
            if (candidates.Count == 0) return 0;
            return candidates.OrderByDescending(x => x.prio).ThenByDescending(x => x.rid).First().val;
        }

        private static BundleSpec? ResolveBestProductBundle(int productId, List<DiscountRule> rules, List<DiscountCampaign> campaigns, DateTime now)
        {
            bool IsCampaignActive(DiscountCampaign dc) =>
                dc.IsActive && now >= dc.StartDate && now <= dc.EndDate;

            var candidates = new List<BundleSpec>();
            foreach (var r in rules)
            {
                if (!r.IsActive || r.ScopeType != "Product" || r.DiscountMode != "Bundle" || r.ProductId != productId)
                    continue;
                if (!r.BundleBuyQty.HasValue || !r.BundlePayQty.HasValue) continue;
                var prio = 0;
                if (r.CampaignId.HasValue)
                {
                    var dc = campaigns.FirstOrDefault(c => c.Id == r.CampaignId.Value);
                    if (dc == null || !IsCampaignActive(dc)) continue;
                    prio = dc.Priority;
                }
                candidates.Add(new BundleSpec(r.BundleBuyQty.Value, r.BundlePayQty.Value, prio, r.Id));
            }
            if (candidates.Count == 0) return null;
            return candidates.OrderByDescending(x => x.Priority).ThenByDescending(x => x.RuleId).First();
        }

        private static decimal ResolveBestClientPercent(int? userId, List<DiscountRule> rules, List<DiscountCampaign> campaigns, DateTime now)
        {
            if (!userId.HasValue) return 0;
            bool IsCampaignActive(DiscountCampaign dc) =>
                dc.IsActive && now >= dc.StartDate && now <= dc.EndDate;

            var candidates = new List<(decimal val, int prio, int rid)>();
            foreach (var r in rules)
            {
                if (!r.IsActive || r.ScopeType != "Client" || r.DiscountMode != "Percent" || r.UserId != userId.Value)
                    continue;
                if (r.CampaignId.HasValue)
                {
                    var dc = campaigns.FirstOrDefault(c => c.Id == r.CampaignId.Value);
                    if (dc == null || !IsCampaignActive(dc)) continue;
                    candidates.Add((r.DiscountValue, dc.Priority, r.Id));
                }
                else
                    candidates.Add((r.DiscountValue, 0, r.Id));
            }
            if (candidates.Count == 0) return 0;
            return candidates.OrderByDescending(x => x.prio).ThenByDescending(x => x.rid).First().val;
        }

        public async Task RemoveFromCartAsync(int productId, int? userId, string? guestId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_RemoveFromCart", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> UpdateQuantityAsync(int productId, int? userId, string? guestId, int change)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_UpdateCartQuantity", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Change", change);
            await connection.OpenAsync();

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task MergeCartsAsync(string guestId, int userId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_MergeCarts", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GuestId", guestId);
            command.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> CreateOrderAsync(Order order, string? guestId, string? promoCode)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_CreateOrder", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@UserId", (object)order.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object)guestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestFirstName", (object)order.GuestFirstName ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestLastName", (object)order.GuestLastName ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestEmail", (object)order.GuestEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestPhone", (object)order.GuestPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", order.Address);
            command.Parameters.AddWithValue("@PromoCode", (object)promoCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeliveryType", (object?)order.DeliveryType ?? DBNull.Value);
            command.Parameters.AddWithValue("@PaymentMethod", (object?)order.PaymentMethod ?? DBNull.Value);

            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<CheckoutPreviewResult?> PreviewCheckoutTotalsAsync(int? userId, string? guestId, string? promoCode, string deliveryType)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_PreviewCheckoutTotals", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", (object?)guestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PromoCode", (object?)promoCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeliveryType", string.IsNullOrEmpty(deliveryType) ? "Courier" : deliveryType);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new CheckoutPreviewResult
            {
                SubtotalAfterProductDiscount = reader.GetDecimal(reader.GetOrdinal("SubtotalAfterProductDiscount")),
                DiscountFromCategoryClientPercent = reader.GetDecimal(reader.GetOrdinal("DiscountFromCategoryClientPercent")),
                GoodsTotalBeforePromo = reader.GetDecimal(reader.GetOrdinal("GoodsTotalBeforePromo")),
                PromoPercent = reader.GetInt32(reader.GetOrdinal("PromoPercent")),
                PromoDiscountAmount = reader.GetDecimal(reader.GetOrdinal("PromoDiscountAmount")),
                GoodsAfterPromo = reader.GetDecimal(reader.GetOrdinal("GoodsAfterPromo")),
                ClientFixedRuleAmount = reader.GetDecimal(reader.GetOrdinal("ClientFixedRuleAmount")),
                CategoryFixedRuleAmount = reader.GetDecimal(reader.GetOrdinal("CategoryFixedRuleAmount")),
                AppliedFixedDiscountAmount = reader.GetDecimal(reader.GetOrdinal("AppliedFixedDiscountAmount")),
                NetGoodsAmount = reader.GetDecimal(reader.GetOrdinal("NetGoodsAmount")),
                DeliveryFee = reader.GetDecimal(reader.GetOrdinal("DeliveryFee")),
                GrandTotal = reader.GetDecimal(reader.GetOrdinal("GrandTotal"))
            };
        }

        public async Task<List<OrderHistoryDto>> GetUserOrdersAsync(int userId)
        {
            var orders = new List<OrderHistoryDto>();
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetUserOrders", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int orderId = reader.GetInt32(reader.GetOrdinal("Id"));
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    order = new OrderHistoryDto
                    {
                        OrderId = orderId,
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                        Status = reader.GetString(reader.GetOrdinal("Status"))
                    };
                    orders.Add(order);
                }

                order.Items.Add(new OrderItemDto
                {
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    Price = reader.GetDecimal(reader.GetOrdinal("HistoricalPrice")),
                    ProductName = reader.GetString(reader.GetOrdinal("Name")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId"))
                });
            }
            return orders;
        }

        public async Task<string?> GetOrderEmailAsync(int orderId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetOrderEmail", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@OrderId", orderId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : result?.ToString();
        }

        public async Task<List<Review>> GetReviewsByProductIdAsync(int productId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetProductReviews", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);

            await connection.OpenAsync();
            var reviews = new List<Review>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) reviews.Add(new Review
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                UserEmail = reader.GetString(reader.GetOrdinal("UserEmail")),
                Rating = reader.GetInt32(reader.GetOrdinal("Rating")) / 2.0,
                Pros = reader.IsDBNull(reader.GetOrdinal("Pros")) ? null : reader.GetString(reader.GetOrdinal("Pros")),
                Cons = reader.IsDBNull(reader.GetOrdinal("Cons")) ? null : reader.GetString(reader.GetOrdinal("Cons")),
                Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
            return reviews;
        }

        public async Task AddReviewAsync(Review review)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddReview", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@ProductId", review.ProductId);
            command.Parameters.AddWithValue("@UserId", (object?)review.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserName", review.UserName);
            command.Parameters.AddWithValue("@UserEmail", review.UserEmail);
            command.Parameters.AddWithValue("@Rating", (int)Math.Round(review.Rating * 2));
            command.Parameters.AddWithValue("@Pros", (object?)review.Pros ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cons", (object?)review.Cons ?? DBNull.Value);
            command.Parameters.AddWithValue("@Comment", (object?)review.Comment ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
        public async Task<(byte[] Data, string ContentType)?> GetMainImageAsync(int productId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetProductMainImage", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (
                    (byte[])reader["ImageData"],
                    reader["ContentType"].ToString() ?? "image/jpeg"
                );
            }
            return null;
        }

        public async Task AddProductImageAsync(ProductImage img)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddProductImage", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@ProductId", img.ProductId);
            command.Parameters.AddWithValue("@ImageData", img.ImageData);
            command.Parameters.AddWithValue("@ContentType", img.ContentType);
            command.Parameters.AddWithValue("@IsMain", img.IsMain);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ProductFeature>> GetFeaturesByCategoryAsync(int categoryId)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetFeaturesByCategory", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            var features = new List<ProductFeature>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                features.Add(new ProductFeature
                {
                    Id = (int)reader["Id"],
                    CategoryId = (int)reader["CategoryId"],
                    Name = reader["Name"].ToString() ?? string.Empty
                });
            }
            return features;
        }
        public async Task<ProductFeature> AddFeatureAsync(int categoryId, string name)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddProductFeature", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Name", name);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ProductFeature
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                };
            }
            throw new Exception("Не удалось получить ID характеристики");
        }
        public async Task UpdateProductConfigurationsAsync(int productId, List<ProductConfiguration> configurations)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var deleteCmd = new SqlCommand("sp_DeleteProductConfigurations", connection)
            { CommandType = CommandType.StoredProcedure };
            deleteCmd.Parameters.AddWithValue("@ProductId", productId);
            await deleteCmd.ExecuteNonQueryAsync();

            if (configurations != null && configurations.Any())
            {
                foreach (var config in configurations)
                {
                    if (string.IsNullOrWhiteSpace(config.FeatureValue)) continue;

                    using var insertCmd = new SqlCommand("sp_AddProductConfiguration", connection)
                    { CommandType = CommandType.StoredProcedure };
                    insertCmd.Parameters.AddWithValue("@ProductId", productId);
                    insertCmd.Parameters.AddWithValue("@FeatureId", config.FeatureId);
                    insertCmd.Parameters.AddWithValue("@FeatureValue", config.FeatureValue);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<CategoryFilterDto>> GetCategoryFiltersAsync(int categoryId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetCategoryFilters", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            var filters = new List<CategoryFilterDto>();

            while (await reader.ReadAsync())
            {
                int featureId = Convert.ToInt32(reader["FeatureId"]);
                string featureName = reader["FeatureName"].ToString() ?? string.Empty;
                string featureValue = reader["FeatureValue"].ToString() ?? string.Empty;

                var existingFilter = filters.FirstOrDefault(f => f.FeatureId == featureId);
                if (existingFilter == null)
                {
                    existingFilter = new CategoryFilterDto { FeatureId = featureId, FeatureName = featureName };
                    filters.Add(existingFilter);
                }

                if (!existingFilter.AvailableValues.Contains(featureValue))
                {
                    existingFilter.AvailableValues.Add(featureValue);
                }
            }
            return filters;
        }


        public async Task<List<PromoCode>> GetAllPromoCodesAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetAllPromoCodes", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();

            var promos = new List<PromoCode>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                promos.Add(new PromoCode
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    DiscountPercent = reader.GetInt32(reader.GetOrdinal("DiscountPercent")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                    EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    MaxUses = TryGetNullableInt32(reader, "MaxUses"),
                    UsedCount = TryGetInt32(reader, "UsedCount", 0),
                    MinGoodsAmount = TryGetNullableDecimal(reader, "MinGoodsAmount")
                });
            }
            return promos;
        }

        public async Task CreatePromoCodeAsync(string code, int discount, DateTime start, DateTime end, int? maxUses = null, decimal? minGoodsAmount = null)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddPromoCode", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@DiscountPercent", discount);
            command.Parameters.AddWithValue("@StartDate", start);
            command.Parameters.AddWithValue("@EndDate", end);
            command.Parameters.AddWithValue("@MaxUses", (object?)maxUses ?? DBNull.Value);
            command.Parameters.AddWithValue("@MinGoodsAmount", (object?)minGoodsAmount ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<PromoCode?> GetPromoCodeByCodeAsync(string code)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetPromoCodeByCode", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Code", code);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new PromoCode
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Code = reader.GetString(reader.GetOrdinal("Code")),
                DiscountPercent = reader.GetInt32(reader.GetOrdinal("DiscountPercent")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                MaxUses = TryGetNullableInt32(reader, "MaxUses"),
                UsedCount = TryGetInt32(reader, "UsedCount", 0),
                MinGoodsAmount = TryGetNullableDecimal(reader, "MinGoodsAmount")
            };
        }

        private static decimal? TryGetNullableDecimal(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetDecimal(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
        private static int? TryGetNullableInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetInt32(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static int TryGetInt32(SqlDataReader reader, string columnName, int defaultValue)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? defaultValue : reader.GetInt32(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return defaultValue;
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetAllOrders", connection) { CommandType = CommandType.StoredProcedure };

            await connection.OpenAsync();
            var orders = new List<Order>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    GuestFirstName = reader.IsDBNull(reader.GetOrdinal("GuestFirstName")) ? null : reader.GetString(reader.GetOrdinal("GuestFirstName")),
                    GuestLastName = reader.IsDBNull(reader.GetOrdinal("GuestLastName")) ? null : reader.GetString(reader.GetOrdinal("GuestLastName")),
                    GuestEmail = reader.IsDBNull(reader.GetOrdinal("GuestEmail")) ? null : reader.GetString(reader.GetOrdinal("GuestEmail")),
                    GuestPhone = reader.IsDBNull(reader.GetOrdinal("GuestPhone")) ? null : reader.GetString(reader.GetOrdinal("GuestPhone")),
                    PromoCodeId = reader.IsDBNull(reader.GetOrdinal("PromoCodeId")) ? null : reader.GetInt32(reader.GetOrdinal("PromoCodeId")),
                    PromoCode = reader.IsDBNull(reader.GetOrdinal("PromoCodeText")) ? null : reader.GetString(reader.GetOrdinal("PromoCodeText"))
                });
            }
            if (await reader.NextResultAsync())
            {
                static int? TryGetOrdinal(IDataRecord r, string name)
                {
                    try
                    {
                        return r.GetOrdinal(name);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return null;
                    }
                }

                while (await reader.ReadAsync())
                {
                    var orderIdOrd = TryGetOrdinal(reader, "OrderId");
                    if (orderIdOrd == null || reader.IsDBNull(orderIdOrd.Value)) continue;
                    int orderId = reader.GetInt32(orderIdOrd.Value);
                    var order = orders.FirstOrDefault(o => o.Id == orderId);
                    if (order == null) continue;

                    decimal price = 0m;
                    var histPriceOrd = TryGetOrdinal(reader, "HistoricalPrice");
                    var priceOrd = TryGetOrdinal(reader, "Price");
                    if (histPriceOrd != null && !reader.IsDBNull(histPriceOrd.Value))
                        price = reader.GetDecimal(histPriceOrd.Value);
                    else if (priceOrd != null && !reader.IsDBNull(priceOrd.Value))
                        price = reader.GetDecimal(priceOrd.Value);

                    string? name = null;
                    var nameOrd = TryGetOrdinal(reader, "Name");
                    var productNameOrd = TryGetOrdinal(reader, "ProductName");
                    if (nameOrd != null && !reader.IsDBNull(nameOrd.Value))
                        name = reader.GetString(nameOrd.Value);
                    else if (productNameOrd != null && !reader.IsDBNull(productNameOrd.Value))
                        name = reader.GetString(productNameOrd.Value);

                    var productIdOrd = TryGetOrdinal(reader, "ProductId");
                    var qtyOrd = TryGetOrdinal(reader, "Quantity");

                    order.Items.Add(new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = productIdOrd == null || reader.IsDBNull(productIdOrd.Value) ? 0 : reader.GetInt32(productIdOrd.Value),
                        Quantity = qtyOrd == null || reader.IsDBNull(qtyOrd.Value) ? 0 : reader.GetInt32(qtyOrd.Value),
                        Price = price,
                        ProductName = name
                    });
                }
            }
            return orders;
        }
        public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_UpdateOrderStatus", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@NewStatus", newStatus);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<OrderStatusReport>> GetOrdersByStatusReportAsync()
        {
            var result = new List<OrderStatusReport>();
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_Report_OrdersByStatus", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new OrderStatusReport
                {
                    Status = reader.IsDBNull(0) ? "Неизвестно" : reader.GetString(0),
                    OrderCount = reader.GetInt32(1)
                });
            }
            return result;
        }

        public async Task<List<CategoryProductReport>> GetProductsByCategoryReportAsync()
        {
            var result = new List<CategoryProductReport>();
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_Report_ProductsByCategory", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new CategoryProductReport
                {
                    CategoryName = reader.IsDBNull(0) ? "Без категории" : reader.GetString(0),
                    ProductCount = reader.GetInt32(1)
                });
            }
            return result;
        }

        public async Task<List<ClientHistoryReport>> GetClientHistoryReportAsync()
        {
            var result = new List<ClientHistoryReport>();
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_Report_ClientHistory", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ClientHistoryReport
                {
                    ClientEmail = reader.GetString(0),
                    ClientName = reader.GetString(1),
                    TotalOrders = reader.GetInt32(2),
                    TotalSpent = reader.GetDecimal(3),
                    LastOrderDate = reader.GetDateTime(4)
                });
            }
            return result;
        }
        public async Task AddFavouriteAsync(int userId, int productId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddFavourite", connection)
            { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveFavouriteAsync(int userId, int productId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_RemoveFavourite", connection)
            { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Product>> GetFavouritesAsync(int userId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetFavourites", connection)
            { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            var products = new List<Product>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) products.Add(MapProduct(reader));
            return products;
        }

        public async Task<bool> IsFavouriteAsync(int userId, int productId)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_IsFavourite", connection)
            { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<List<DiscountCampaign>> GetDiscountCampaignsAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetDiscountCampaigns", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();
            var list = new List<DiscountCampaign>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DiscountCampaign
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                    EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            return list;
        }

        public async Task<int> AddDiscountCampaignAsync(string name, string? description, DateTime startDate, DateTime endDate, bool isActive, int priority)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddDiscountCampaign", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.Parameters.AddWithValue("@IsActive", isActive);
            command.Parameters.AddWithValue("@Priority", priority);
            await connection.OpenAsync();
            var scalar = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalar);
        }

        public async Task UpdateDiscountCampaignAsync(int id, string name, string? description, DateTime startDate, DateTime endDate, bool isActive, int priority)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_UpdateDiscountCampaign", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.Parameters.AddWithValue("@IsActive", isActive);
            command.Parameters.AddWithValue("@Priority", priority);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDiscountCampaignAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_DeleteDiscountCampaign", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<DiscountRule>> GetDiscountRulesAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetDiscountRules", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();
            var list = new List<DiscountRule>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DiscountRule
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CampaignId = TryGetNullableInt32(reader, "CampaignId"),
                    ScopeType = reader.GetString(reader.GetOrdinal("ScopeType")),
                    ProductId = TryGetNullableInt32(reader, "ProductId"),
                    CategoryId = TryGetNullableInt32(reader, "CategoryId"),
                    UserId = TryGetNullableInt32(reader, "UserId"),
                    DiscountMode = reader.GetString(reader.GetOrdinal("DiscountMode")),
                    DiscountValue = reader.GetDecimal(reader.GetOrdinal("DiscountValue")),
                    MinGoodsAmount = TryGetNullableDecimal(reader, "MinGoodsAmount"),
                    BundleBuyQty = TryGetNullableInt32(reader, "BundleBuyQty"),
                    BundlePayQty = TryGetNullableInt32(reader, "BundlePayQty"),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    CategoryName = TryGetNullableString(reader, "CategoryName"),
                    ProductName = TryGetNullableString(reader, "ProductName"),
                    CampaignName = TryGetNullableString(reader, "CampaignName")
                });
            }
            return list;
        }

        public async Task<int> AddDiscountRuleAsync(int? campaignId, string scopeType, int? productId, int? categoryId, int? userId, string discountMode, decimal discountValue, decimal? minGoodsAmount, int? bundleBuyQty, int? bundlePayQty, bool isActive)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddDiscountRule", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CampaignId", (object?)campaignId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ScopeType", scopeType);
            command.Parameters.AddWithValue("@ProductId", (object?)productId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DiscountMode", discountMode);
            command.Parameters.AddWithValue("@DiscountValue", discountValue);
            command.Parameters.AddWithValue("@MinGoodsAmount", (object?)minGoodsAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@BundleBuyQty", (object?)bundleBuyQty ?? DBNull.Value);
            command.Parameters.AddWithValue("@BundlePayQty", (object?)bundlePayQty ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            var scalar = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalar);
        }

        public async Task UpdateDiscountRuleAsync(int id, int? campaignId, string scopeType, int? productId, int? categoryId, int? userId, string discountMode, decimal discountValue, decimal? minGoodsAmount, int? bundleBuyQty, int? bundlePayQty, bool isActive)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_UpdateDiscountRule", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@CampaignId", (object?)campaignId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ScopeType", scopeType);
            command.Parameters.AddWithValue("@ProductId", (object?)productId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DiscountMode", discountMode);
            command.Parameters.AddWithValue("@DiscountValue", discountValue);
            command.Parameters.AddWithValue("@MinGoodsAmount", (object?)minGoodsAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@BundleBuyQty", (object?)bundleBuyQty ?? DBNull.Value);
            command.Parameters.AddWithValue("@BundlePayQty", (object?)bundlePayQty ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDiscountRuleAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_DeleteDiscountRule", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Id", id);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task ApplyCatalogDisplayPricesAsync(IList<Product> products, int? userId)
        {
            if (products == null || products.Count == 0) return;

            var rules = await GetDiscountRulesAsync();
            var campaigns = await GetDiscountCampaignsAsync();
            var now = DateTime.Now;

            var clientPct = ResolveBestClientPercent(userId, rules, campaigns, now);

            foreach (var p in products)
            {
                var prodPct = ResolveBestProductPercent(p.Id, rules, campaigns, now);
                var catPct = ResolveBestCategoryPercent(p.CategoryId, rules, campaigns, now);
                var stackPct = Math.Max(prodPct, Math.Max(catPct, clientPct));
                var final = p.Price * (1 - stackPct / 100m);
                final = Math.Round(final, 2, MidpointRounding.AwayFromZero);

                p.CatalogSalePrice = final;
                if (final < p.Price - 0.005m)
                {
                    p.CatalogCompareAtPrice = p.Price;
                    var offPct = (p.Price - final) / p.Price * 100m;
                    var rounded = (int)Math.Round(offPct, MidpointRounding.AwayFromZero);
                    if (offPct > 0 && rounded == 0) rounded = 1;
                    if (rounded > 100) rounded = 100;
                    p.CatalogDiscountBadgePercent = rounded;
                }
                else
                {
                    p.CatalogCompareAtPrice = null;
                    p.CatalogDiscountBadgePercent = null;
                }
            }
        }

        public async Task<object> GetDiscountStatusForProductAsync(int productId)
        {
            var rules = await GetDiscountRulesAsync();
            var campaigns = await GetDiscountCampaignsAsync();
            var now = DateTime.Now;

            int categoryId;
            string? productName;
            using (var connection = _factory.CreateConnection())
            {
                await connection.OpenAsync();
                using var cmd = new SqlCommand("SELECT TOP 1 Name, CategoryId FROM dbo.Products WHERE Id=@Id", connection);
                cmd.Parameters.AddWithValue("@Id", productId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return new { productId, exists = false };
                productName = reader.IsDBNull(0) ? null : reader.GetString(0);
                categoryId = reader.GetInt32(1);
            }

            var prodPct = ResolveBestProductPercent(productId, rules, campaigns, now);
            var catPct = ResolveBestCategoryPercent(categoryId, rules, campaigns, now);
            var bestPct = Math.Max(prodPct, catPct);

            var bundle = ResolveBestProductBundle(productId, rules, campaigns, now);

            var source = bestPct <= 0
                ? null
                : (prodPct >= catPct ? "Product" : "Category");

            return new
            {
                productId,
                exists = true,
                productName,
                categoryId,
                bestPercent = bestPct,
                bestPercentSource = source,
                hasBundle = bundle != null,
                bundleBuyQty = bundle?.BuyQty,
                bundlePayQty = bundle?.PayQty
            };
        }

        private static string? TryGetNullableString(SqlDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetString(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private Product MapProduct(SqlDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                ArticleNumber = reader.IsDBNull(reader.GetOrdinal("ArticleNumber")) ? null : reader.GetString(reader.GetOrdinal("ArticleNumber")),
                ShortDescription = reader.IsDBNull(reader.GetOrdinal("ShortDescription")) ? null : reader.GetString(reader.GetOrdinal("ShortDescription")),
                FullDescription = reader.IsDBNull(reader.GetOrdinal("FullDescription")) ? null : reader.GetString(reader.GetOrdinal("FullDescription")),
                AverageRating = reader.IsDBNull(reader.GetOrdinal("AverageRating")) ? 0 : Convert.ToDouble(reader["AverageRating"]),
                ReviewsCount = reader.IsDBNull(reader.GetOrdinal("ReviewsCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReviewsCount"))
            };
        }
    }
}