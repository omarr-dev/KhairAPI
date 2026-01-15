using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using KhairAPI.Data;
using KhairAPI.Core.Extensions;
using KhairAPI.Core.Middleware;
using Microsoft.OpenApi.Models;
using Hangfire;
using Hangfire.PostgreSql;
using KhairAPI.Services.Interfaces;

namespace KhairAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add controllers
        builder.Services.AddControllers();

        // Configure PostgreSQL with Entity Framework
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Configure CORS for Next.js frontend
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("NextJsPolicy", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    // Allow localhost and subdomains (e.g., khair.localhost:3000)
                    if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                        return true;
                    
                    // Allow *.localhost for multi-tenancy subdomain testing
                    if (origin.Contains(".localhost:") || origin.EndsWith(".localhost") )
                        return true;

                    // Allow production domain maarij.sa and its subdomains
                    if (origin == "https://maarij.sa" || origin.EndsWith(".maarij.sa"))
                        return true;
                    
                    // Allow Vercel deployments
                    if (origin.Contains(".vercel.app"))
                        return true;
                    
                    return false;
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
        });

        // Configure JWT Authentication
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"));

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("SupervisorOnly", policy => policy.RequireRole("Supervisor"));
            options.AddPolicy("TeacherOrSupervisor", policy => policy.RequireRole("Teacher", "Supervisor"));
        });

        // Configure Swagger/OpenAPI with JWT support
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Khair API - نظام إدارة الحلقات القرآنية",
                Version = "v1",
                Description = "API لنظام إدارة حلقات تحفيظ القرآن الكريم"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Add AutoMapper
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Register all application services using extension method
        builder.Services.AddApplicationServices();

        // Configure Hangfire with PostgreSQL storage
        builder.Services.AddHangfireServices(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Khair API v1");
            });
        }

        // Add global exception handler middleware
        app.UseMiddleware<GlobalExceptionHandler>();

        app.UseHttpsRedirection();

        app.UseCors("NextJsPolicy");

        app.UseAuthentication();

        // Multi-tenancy: Extract tenant context from JWT
        app.UseMiddleware<KhairAPI.Middleware.TenantMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        // Configure Hangfire Dashboard
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            DashboardTitle = "Khair - إدارة المهام المجدولة"
        });

            // Apply migrations automatically on startup (all environments)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AppDbContext>();
                    dbContext.Database.Migrate();

                    // Seed data only in development
                    if (app.Environment.IsDevelopment())
                    {
                        await SeedData.InitializeAsync(services);
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during database migration or seeding.");
                    // Re-throw to prevent application from starting in an inconsistent state
                    throw;
                }
            }

            // Safe timezone retrieval for cross-platform compatibility (Linux/Mac/Windows)
            TimeZoneInfo ksaTimeZone;
            try
            {
                ksaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");
            }
            catch (TimeZoneNotFoundException)
            {
                ksaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
            }

            // Schedule recurring job to mark absent students daily at 23:59 KSA time
            RecurringJob.AddOrUpdate<IAttendanceBackgroundService>(
                "mark-absent-students",
                service => service.MarkAbsentForMissingAttendanceAsync(null),
                "59 23 * * *",
                new RecurringJobOptions
                {
                    TimeZone = ksaTimeZone
                });

        app.Run();
    }
}
