using HouseholdStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace HouseholdStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllersWithViews();

            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<AuthApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7152/api/auth/");
            });
            builder.Services.AddHttpClient<ProductApiService>();
            builder.Services.AddScoped<ProductApiService>();



            builder.Services.AddSingleton<Toolify.ProductService.Database.SqlConnectionFactory>(sp =>
                    new Toolify.ProductService.Database.SqlConnectionFactory(builder.Configuration));

            builder.Services.AddScoped<Toolify.ProductService.Data.ProductRepository>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/Login";
                });

            builder.Services.AddAuthorization();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); 
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });



            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); 
            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}
