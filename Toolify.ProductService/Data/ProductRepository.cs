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

        // --- PRODUCTS ---
        public async Task<List<Product>> GetAllAsync()
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_GetAllProducts", connection) { CommandType = CommandType.StoredProcedure };
            await connection.OpenAsync();

            var products = new List<Product>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) products.Add(MapProduct(reader));
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

        // Самый важный метод. Получает товар, конфигурации и картинки за 1 запрос
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
            foreach (var config in product.Configurations) featureTable.Rows.Add(config.FeatureId, config.FeatureValue);

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

        // --- CATEGORIES ---
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
            using var command = new SqlCommand("sp_AddCategory", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Name", category.Name);

            await connection.OpenAsync();
            category.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
            return category;
        }

        // --- CART ---
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

        // --- ORDERS ---
        public async Task<int> CreateOrderAsync(Order order, string guestId, string? promoCode = null)
        {
            using var connection = _factory.CreateConnection();
            using var command = new SqlCommand("sp_CreateOrder", connection) { CommandType = CommandType.StoredProcedure };

            command.Parameters.AddWithValue("@UserId", (object?)order.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuestFirstName", order.GuestFirstName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GuestLastName", order.GuestLastName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GuestEmail", order.GuestEmail ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GuestPhone", order.GuestPhone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Address", order.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@GuestId", guestId);
            command.Parameters.AddWithValue("@PromoCode", promoCode ?? (object)DBNull.Value);

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
                int orderId = reader.GetInt32(0);
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    order = new OrderHistoryDto
                    {
                        OrderId = orderId,
                        OrderDate = reader.GetDateTime(1),
                        TotalAmount = reader.GetDecimal(2),
                        Status = reader.GetString(3)
                    };
                    orders.Add(order);
                }

                order.Items.Add(new OrderItemDto
                {
                    Quantity = reader.GetInt32(4),
                    Price = reader.GetDecimal(5),
                    ProductName = reader.GetString(6)
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

        // --- REVIEWS ---
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
                Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
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
            command.Parameters.AddWithValue("@Rating", review.Rating);
            command.Parameters.AddWithValue("@Pros", (object?)review.Pros ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cons", (object?)review.Cons ?? DBNull.Value);
            command.Parameters.AddWithValue("@Comment", (object?)review.Comment ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
        // Получение байтов изображения
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

        // Сохранение изображения (для админки)
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


        // --- ХЕЛПЕР ---
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