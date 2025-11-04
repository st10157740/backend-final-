using MbabaneHighlandersBackend2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MbabaneHighlandersBackend2.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Resend;


namespace MbabaneHighlandersBackend2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database context
            builder.Services.AddDbContext<MbabaneHighlandersBackend2Context>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("MbabaneHighlandersBackend2Context")
                    ?? throw new InvalidOperationException("Connection string 'MbabaneHighlandersBackend2Context' not found.")
                ));

            // Identity + Role support
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<MbabaneHighlandersBackend2Context>()
                .AddDefaultTokenProviders();

            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key missing")))
                    };
                });

            // Optional cookie config
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Injected services
            builder.Services.AddHttpClient<IFlutterwaveService, FlutterwaveService>();
            builder.Services.AddScoped<IBlobUpload, BlobUpload>();
            builder.Services.AddScoped<IEmailService, SendEmailService>();
            builder.Services.AddHttpClient<ResendClient>();
            builder.Services.AddTransient<IResend>(sp =>
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<ResendClientOptions>>();
                var httpClient = sp.GetRequiredService<HttpClient>();
                return new ResendClient(options, httpClient);
            });

            // CORS for React frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    policy => policy.WithOrigins("https://mbabanehighlandersam.co.sz")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowCredentials());
            });

            var app = builder.Build();

            app.UseCors("AllowReactApp");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Seed roles and admin user
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await SeedRolesAndAdmin(services);
            }

            app.Run();
        }

        private static async Task SeedRolesAndAdmin(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "admin", "user" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            string adminEmail = "admin@highlanders.com";
            string adminPassword = "Admin@12345";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var user = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "admin");
                    Console.WriteLine("Admin user seeded.");
                }
                else
                {
                    Console.WriteLine("Failed to seed admin user:");
                    foreach (var error in result.Errors)
                        Console.WriteLine($" - {error.Description}");
                }
            }
        }
    }
}