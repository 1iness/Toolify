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
                INSERT INTO Products (CategoryId, Name, ShortDescription, FullDescription, Price, ImagePath)
                VALUES (@CategoryId, @Name, @ShortDescription, @FullDescription, @Price, @ImagePath);
                SELECT SCOPE_IDENTITY();
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@ShortDescription", (object?)product.ShortDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@FullDescription", (object?)product.FullDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ImagePath", (object?)product.ImagePath ?? DBNull.Value);

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
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath"))? null: reader.GetString(reader.GetOrdinal("ImagePath"))
            };
        }
    }
}