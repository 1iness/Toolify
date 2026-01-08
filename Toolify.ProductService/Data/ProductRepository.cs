using Toolify.ProductService.Database;
using Toolify.ProductService.Models;
using System.Data.SqlClient;

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
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT * FROM Products", connection);

            var products = new List<Product>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }

            return products;
        }
        
        public async Task<List<Product>> SearchAsync(string term)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = @"
                SELECT * FROM Products 
                WHERE Name LIKE @Term OR ArticleNumber LIKE @Term
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Term", $"%{term}%");

            var products = new List<Product>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapProduct(reader));
            }
            return products;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT Id, Name FROM Categories", connection);
            var categories = new List<Category>();

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

        public async Task<Category> AddCategoryAsync(Category category)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = "INSERT INTO Categories (Name) VALUES (@Name); SELECT SCOPE_IDENTITY();";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", category.Name);

            var id = await command.ExecuteScalarAsync();
            category.Id = Convert.ToInt32(id);
            return category;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT * FROM Products WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapProduct(reader);
            }

            return null;
        }

        public async Task<int> AddAsync(Product product)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Products (CategoryId, Name, ShortDescription, FullDescription, Price, ImagePath, StockQuantity, Discount, ArticleNumber)
                VALUES (@CategoryId, @Name, @ShortDescription, @FullDescription, @Price, @ImagePath, @StockQuantity, @Discount, @ArticleNumber);
                SELECT SCOPE_IDENTITY();
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@ShortDescription", (object?)product.ShortDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@FullDescription", (object?)product.FullDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ImagePath", (object?)product.ImagePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@Discount", product.Discount);
            command.Parameters.AddWithValue("@ArticleNumber", (object?)product.ArticleNumber ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        public async Task<bool> UpdateAsync(Product product)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = @"
                UPDATE Products
                SET CategoryId = @CategoryId,
                    Name = @Name,
                    ShortDescription = @ShortDescription,
                    FullDescription = @FullDescription,
                    Price = @Price,
                    ImagePath = @ImagePath,
                    StockQuantity = @StockQuantity,
                    Discount = @Discount,
                    ArticleNumber = @ArticleNumber,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@ShortDescription", (object?)product.ShortDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@FullDescription", (object?)product.FullDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ImagePath", (object?)product.ImagePath ?? DBNull.Value);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@Discount", product.Discount);
            command.Parameters.AddWithValue("@ArticleNumber", (object?)product.ArticleNumber ?? DBNull.Value);


            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private Product MapProduct(SqlDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                ShortDescription = reader.IsDBNull(reader.GetOrdinal("ShortDescription")) ? null : reader.GetString(reader.GetOrdinal("ShortDescription")),
                FullDescription = reader.IsDBNull(reader.GetOrdinal("FullDescription")) ? null : reader.GetString(reader.GetOrdinal("FullDescription")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                Discount = reader.GetInt32(reader.GetOrdinal("Discount")),
                ArticleNumber = reader.IsDBNull(reader.GetOrdinal("ArticleNumber")) ? null : reader.GetString(reader.GetOrdinal("ArticleNumber"))
            };
        }
        public void AddToCart(int productId, int? userId, string? guestId, int quantity = 1)
        {
            using var connection = _factory.CreateConnection();
            connection.Open();

            string checkSql = @"
        SELECT Id, Quantity FROM CartItems 
        WHERE ProductId = @pid AND (@uid IS NOT NULL AND UserId = @uid OR @gid IS NOT NULL AND GuestId = @gid)";

            using var cmd = new SqlCommand(checkSql, connection);
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int entryId = reader.GetInt32(0);
                reader.Close();
                string updateSql = "UPDATE CartItems SET Quantity = Quantity + @q WHERE Id = @id";
                using var upCmd = new SqlCommand(updateSql, connection);
                upCmd.Parameters.AddWithValue("@q", quantity);
                upCmd.Parameters.AddWithValue("@id", entryId);
                upCmd.ExecuteNonQuery();
            }
            else
            {
                reader.Close();
                string insertSql = @"
            INSERT INTO CartItems (ProductId, UserId, GuestId, Quantity) 
            VALUES (@pid, @uid, @gid, @q)";
                using var inCmd = new SqlCommand(insertSql, connection);
                inCmd.Parameters.AddWithValue("@pid", productId);
                inCmd.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                inCmd.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);
                inCmd.Parameters.AddWithValue("@q", quantity);
                inCmd.ExecuteNonQuery();
            }
        }
        public async Task<List<CartItem>> GetCartItemsAsync(int? userId, string? guestId)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = @"
        SELECT c.ProductId, p.Name, p.Price, p.ImagePath, c.Quantity, p.Discount
        FROM CartItems c
        JOIN Products p ON c.ProductId = p.Id
        WHERE (@uid IS NOT NULL AND c.UserId = @uid) 
           OR (@uid IS NULL AND @gid IS NOT NULL AND c.GuestId = @gid)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);

            var items = new List<CartItem>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var basePrice = reader.GetDecimal(reader.GetOrdinal("Price"));
                var discount = reader.GetInt32(reader.GetOrdinal("Discount"));

                decimal discountAmount = basePrice * (discount / 100m); 
                decimal finalPrice = basePrice - discountAmount;

                items.Add(new CartItem
                {
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    ProductName = reader.GetString(reader.GetOrdinal("Name")),
                    Price = finalPrice, 
                    OldPrice = discount > 0 ? basePrice : null, 
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity"))
                });
            }
            return items;
        }
        public async Task RemoveFromCartAsync(int productId, int? userId, string? guestId)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();
            string sql = "DELETE FROM CartItems WHERE ProductId = @pid AND (@uid IS NOT NULL AND UserId = @uid OR GuestId = @gid)";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> UpdateQuantityAsync(int productId, int? userId, string? guestId, int change)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sqlCheck = @"
        SELECT c.Quantity, p.StockQuantity
        FROM CartItems c
        JOIN Products p ON c.ProductId = p.Id
        WHERE c.ProductId = @pid 
          AND (c.UserId = @uid OR (c.UserId IS NULL AND c.GuestId = @gid))";

            int currentCartQuantity = 0;
            int stockQuantity = 0;

            using (var cmdCheck = new SqlCommand(sqlCheck, connection))
            {
                cmdCheck.Parameters.AddWithValue("@pid", productId);
                cmdCheck.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                cmdCheck.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);

                using (var reader = await cmdCheck.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        currentCartQuantity = reader.GetInt32(0);
                        stockQuantity = reader.GetInt32(1);
                    }
                    else
                    {
                        return false; 
                    }
                }
            }

            int newQuantity = currentCartQuantity + change;

            if (newQuantity < 1) return false;

            if (newQuantity > stockQuantity)
            {
                return false; 
            }

            string sqlUpdate = @"
        UPDATE CartItems 
        SET Quantity = @qty 
        WHERE ProductId = @pid 
          AND (UserId = @uid OR (UserId IS NULL AND GuestId = @gid))";

            using (var cmdUpdate = new SqlCommand(sqlUpdate, connection))
            {
                cmdUpdate.Parameters.AddWithValue("@qty", newQuantity);
                cmdUpdate.Parameters.AddWithValue("@pid", productId);
                cmdUpdate.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
                cmdUpdate.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);

                await cmdUpdate.ExecuteNonQueryAsync();
            }

            return true;
        }

        public async Task MergeCartsAsync(string guestId, int userId)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();


            string sql = @"
            UPDATE CartItems SET UserId = @uid, GuestId = NULL 
            WHERE GuestId = @gid AND ProductId NOT IN (SELECT ProductId FROM CartItems WHERE UserId = @uid);
        
            DELETE FROM CartItems WHERE GuestId = @gid; -- Удаляются дубликаты, которые не смогли обновиться(не удалять пометку эту!!!!!!)";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@gid", guestId);
            cmd.Parameters.AddWithValue("@uid", userId);
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task<int> CreateOrderAsync(Order order, string guestId)
        {
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var cartItems = new List<CartItem>();
                string getCartSql = @"
            SELECT c.ProductId, c.Quantity, p.Price, p.Discount
            FROM CartItems c
            JOIN Products p ON c.ProductId = p.Id
            WHERE (@uid IS NOT NULL AND c.UserId = @uid) 
               OR (@uid IS NULL AND @gid IS NOT NULL AND c.GuestId = @gid)";

                using (var cmd = new SqlCommand(getCartSql, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@uid", (object?)order.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var basePrice = reader.GetDecimal(2);
                        var discount = reader.GetInt32(3);
                        decimal finalPrice = discount > 0 ? basePrice * (1 - (decimal)discount / 100) : basePrice;

                        cartItems.Add(new CartItem
                        {
                            ProductId = reader.GetInt32(0),
                            Quantity = reader.GetInt32(1),
                            Price = finalPrice
                        });
                    }
                }

                if (cartItems.Count == 0) return 0;

                decimal totalAmount = cartItems.Sum(x => x.Price * x.Quantity);

                string createOrderSql = @"
            INSERT INTO Orders (UserId, GuestFirstName, GuestLastName, GuestEmail, GuestPhone, OrderDate, TotalAmount, Status, Address) 
            VALUES (@uid, @fn, @ln, @em, @ph, GETDATE(), @total, 'New', @addr);
            SELECT SCOPE_IDENTITY();";

                int newOrderId;
                using (var cmd = new SqlCommand(createOrderSql, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@uid", (object?)order.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@fn", order.GuestFirstName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ln", order.GuestLastName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@em", order.GuestEmail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ph", order.GuestPhone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@total", totalAmount);
                    cmd.Parameters.AddWithValue("@addr", order.Address ?? (object)DBNull.Value);

                    newOrderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                string addItemSql = "INSERT INTO OrderItems (OrderId, ProductId, Price, Quantity) VALUES (@oid, @pid, @price, @q)";
                foreach (var item in cartItems)
                {
                    using var cmd = new SqlCommand(addItemSql, connection, transaction);
                    cmd.Parameters.AddWithValue("@oid", newOrderId);
                    cmd.Parameters.AddWithValue("@pid", item.ProductId);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    cmd.Parameters.AddWithValue("@q", item.Quantity);
                    await cmd.ExecuteNonQueryAsync();
                }

                string clearCartSql = @"DELETE FROM CartItems WHERE (@uid IS NOT NULL AND UserId = @uid) OR (@gid IS NOT NULL AND GuestId = @gid)";
                using (var cmd = new SqlCommand(clearCartSql, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@uid", (object?)order.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gid", (object?)guestId ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return newOrderId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<OrderHistoryDto>> GetUserOrdersAsync(int userId)
        {
            var orders = new List<OrderHistoryDto>();
            using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            string sql = @"
        SELECT o.Id, o.OrderDate, o.TotalAmount, o.Status,
               oi.Quantity, oi.Price, p.Name, p.ImagePath
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        JOIN Products p ON oi.ProductId = p.Id
        WHERE o.UserId = @uid
        ORDER BY o.OrderDate DESC";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int orderId = reader.GetInt32(0);
                var existingOrder = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (existingOrder == null)
                {
                    existingOrder = new OrderHistoryDto
                    {
                        OrderId = orderId,
                        OrderDate = reader.GetDateTime(1),
                        TotalAmount = reader.GetDecimal(2),
                        Status = reader.GetString(3)
                    };
                    orders.Add(existingOrder);
                }

                existingOrder.Items.Add(new OrderItemDto
                {
                    Quantity = reader.GetInt32(4),
                    Price = reader.GetDecimal(5),
                    ProductName = reader.GetString(6),
                    ImageUrl = reader.IsDBNull(7) ? "/images/no-image.png" : reader.GetString(7)
                });
            }
            return orders;
        }
    }
}