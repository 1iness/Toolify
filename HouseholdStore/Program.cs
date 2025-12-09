using HouseholdStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HouseholdStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<AuthApiService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
