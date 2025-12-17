
using Toolify.ProductService.Data;
using Toolify.ProductService.Database;
using Toolify.ProductService.Services;

namespace Toolify.ProductService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<SqlConnectionFactory>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new SqlConnectionFactory(config);
            });

            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<ProductManager>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
           
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
