using System.Data.SqlClient;

namespace Toolify.ProductService.Database
{
    public class SqlConnectionFactory 
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ProductDb");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
