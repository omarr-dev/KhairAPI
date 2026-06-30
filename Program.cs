using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using KhairAPI.Data;
using KhairAPI.Core.Extensions;
using static KhairAPI.Core.Extensions.CachingExtensions;
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
        // Fix for Npgsql 6.0+ DateTime issue: "Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported"
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var builder = WebApplication.CreateBuilder(args);

        // Sentry error monitoring. Options (Dsn, TracesSampleRate, etc.) are read
        // from the "Sentry" section in appsettings.json. The environment is taken
        // from ASPNETCORE_ENVIRONMENT automatically.
        builder.WebHost.UseSentry();

        // Add controllers
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Serialize/deserialize enums (e.g. UserRole) as their string names
                // so the frontend can send "HalaqaSupervisor" instead of an integer.
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // Response compression (large JSON payloads: hierarchy, follow-up, statistics)
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });
        builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(o =>
            o.Level = System.IO.Compression.CompressionLevel.Fastest);
        builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(o =>
            o.Level = System.IO.Compression.CompressionLevel.Fastest);

        // Configure PostgreSQL with Entity Framework.
        // Cap the pool so EF + Hangfire together stay under the server's
        // session-mode pooler limit (15). EF gets 8, Hangfire gets 4.
        var efConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(
            builder.Configuration.GetConnectionString("DefaultConnection"))
        {
            MaxPoolSize = 8,
            MinPoolSize = 1,
            ConnectionIdleLifetime = 60
        }.ConnectionString;
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(efConnectionString));

        // Configure CORS for Next.js frontend
      // Add CORS service to allow all origins, headers, and methods
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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
            // HalaqaSupervisor can do most supervisor tasks within their assigned halaqas
            options.AddPolicy("HalaqaSupervisorOrHigher", policy => policy.RequireRole("Supervisor", "HalaqaSupervisor"));
            // Any authenticated user with a valid role
            options.AddPolicy("AnyRole", policy => policy.RequireRole("Supervisor", "HalaqaSupervisor", "Teacher"));
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

        // Add caching for scalability 
        builder.Services.AddCachingServices();

        // Register all application services using extension method
        builder.Services.AddApplicationServices();

        // Configure Hangfire with PostgreSQL storage
        builder.Services.AddHangfireServices(builder.Configuration);

        var app = builder.Build();
        app.UseResponseCompression();
        app.UseCors();

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
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();

            // Seed data only in development
            if (app.Environment.IsDevelopment())
            {
                await SeedData.InitializeAsync(scope.ServiceProvider);
            }
        }

        // Schedule recurring job to mark absent students daily at 23:59 KSA time
        RecurringJob.AddOrUpdate<IAttendanceBackgroundService>(
            "mark-absent-students",
            service => service.MarkAbsentForMissingAttendanceAsync(null),
            "59 23 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time")
            });

        // Schedule recurring job to reset streaks for missed targets daily at 23:59 KSA time
        RecurringJob.AddOrUpdate<IAttendanceBackgroundService>(
            "reset-streaks-for-missed-targets",
            service => service.ResetStreaksForMissedTargetsAsync(null),
            "59 23 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time")
            });

        app.Run();
    }
}
