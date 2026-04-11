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
            command.Parameters.AddWithValue("@Discount", product.Discount);
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
            command.Parameters.AddWithValue("@Discount", product.Discount);
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
            var items = new List<CartItem>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var basePrice = reader.GetDecimal(reader.GetOrdinal("Price"));
                var discount = reader.GetInt32(reader.GetOrdinal("Discount"));
                decimal finalPrice = discount > 0 ? basePrice * (1 - discount / 100m) : basePrice;

                items.Add(new CartItem
                {
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    ProductName = reader.GetString(reader.GetOrdinal("Name")),
                    Price = finalPrice,
                    OldPrice = discount > 0 ? basePrice : null,
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    ArticleNumber = reader.IsDBNull(reader.GetOrdinal("ArticleNumber")) ? null : reader.GetString(reader.GetOrdinal("ArticleNumber"))
                });
            }
            return items;
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

            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
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
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return promos;
        }

        public async Task CreatePromoCodeAsync(string code, int discount, DateTime start, DateTime end)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_AddPromoCode", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@DiscountPercent", discount);
            command.Parameters.AddWithValue("@StartDate", start);
            command.Parameters.AddWithValue("@EndDate", end);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
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



        private Product MapProduct(SqlDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                ArticleNumber = reader.IsDBNull(reader.GetOrdinal("ArticleNumber")) ? null : reader.GetString(reader.GetOrdinal("ArticleNumber")),
                ShortDescription = reader.IsDBNull(reader.GetOrdinal("ShortDescription")) ? null : reader.GetString(reader.GetOrdinal("ShortDescription")),
                FullDescription = reader.IsDBNull(reader.GetOrdinal("FullDescription")) ? null : reader.GetString(reader.GetOrdinal("FullDescription")),
                AverageRating = reader.IsDBNull(reader.GetOrdinal("AverageRating")) ? 0 : Convert.ToDouble(reader["AverageRating"]),
                ReviewsCount = reader.IsDBNull(reader.GetOrdinal("ReviewsCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReviewsCount"))
            };
        }
    }
}